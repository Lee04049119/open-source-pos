import { Component, Input, OnInit, ViewChild, ElementRef } from '@angular/core';
import { posTrans, posItemRow, posItem, POS, InvoiceMaster, InvoiceDetailItems, InvoiceMasterListing, InvoiceDetailItemEditModel } from '../../models/posTrans';
import { PosService } from './pos.service';
import { Configuration } from '../../app.constants';
import { AuthService } from '../login/auth.service';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import {MessageModule} from 'primeng/message';

@Component({
  selector: 'app-pos',
  templateUrl: 'pos.component.html',
  providers: [MessageService]
})
export class PosComponent  implements OnInit  {
  disabled:any = true;
  //clonedCars: { [s: string]: Car; } = {};
  @ViewChild('hrBeforeDtl') hrBeforeDtlRef: ElementRef | undefined;  

  posItemRows: posItemRow[] =  [];  
  form: posTrans  = {
    InvoiceNo: 0,
    InvoiceDate: new Date(),
    Cashier: "",
    POSCode: "",
    ModuleID: '',
    CompanyID: 0,
    InvoiceType: '',
    FiscalYearID: '',
    BranchID: 0
  };  
  displayItemSearchModal:boolean = false;  
  displayInvoiceTotalModal:boolean = false;  
  posItemSearch:posItem[] = [];  
  selectedItemFromSearch:posItem = {
    Id: "",
    CustomCode: "",
    ItemId: "",
    Description: "",
    ShortDesc: "",
    SalePrice: 0
  };  
  SelectedIndexOfDtl:number = 0;  
  txtTotalAmount:number = 0;  
  modelTotalAmount: number = 0;  
  discountPercnt: number = 0;  
  netAmount: number = 0;  
  receiveAmount: number = 0 ;  
  balanceAmount: number = 0;  
  posItemSearchKeyUptime:number = 0;  
  posItemSearchKeyUpPrevTime:number = 0;  
  txtSearch: string = "";  
  DateTimeFormate:string = "";
  currentUser:any;  

    /**Is the item is being updated or created */
  @Input()  isEditing:boolean = false;
  @Input()  showInv!: InvoiceMasterListing;

  /** Whether the invoice is posted or not */
  isPosted = false;
  constructor(private authService: AuthService,private posService: PosService, private router: Router, private _configuration: Configuration,  private messageService: MessageService) { 
    this.DateTimeFormate = Configuration.DateTimeFormate;
    this.currentUser = this.authService.GetlocalStorageUser();
  }

    ngOnInit() {      
      if(this.isEditing){
        
        this.isPosted = this.showInv?.Status! === "P"
        
        debugger;
        this.form = {
          InvoiceNo: this.showInv?.InvoiceNo,
          InvoiceDate: this.showInv?.InvoiceDate,
          Cashier: this.showInv?.UserName!,
          POSCode: "02",
          ModuleID: 'POS',
          CompanyID: this.showInv?.CompanyID!,
          InvoiceType: this.showInv?.InvoiceType,
          FiscalYearID: this.showInv?.FiscalYearID,
          BranchID: 1
        };
        this.displayItemSearchModal = false;
        this.receiveAmount = 0;
        this.discountPercnt = 0;
        this.posItemSearchKeyUptime = new Date().getTime();
        this.posItemSearchKeyUpPrevTime = 0;
        this.txtTotalAmount = 0;
        this.posService.GetInvoiceDetails(this.showInv).subscribe({
          next: (sr) => {
            debugger;                          
            this.posItemRows = [];
            sr.Data.forEach((element: InvoiceDetailItems) => {
              this.txtTotalAmount += element.InvoiceValue!;
              let row = this.RevMapInvoiceDetailItems(element);
              row.IsInserted = true;
              this.posItemRows.push(row);              
            });  
            if(!this.isPosted){
              this.AddNewRowToDetail();
            }
            
          },
          error:(error) =>{
            debugger;          
            console.error(error);
            this.messageService.add({severity:'error', summary: error.Title, detail: error.Message, life: 3000});
          }});


      }
      else{
        this.RefreshForm();
      }
      
    }
    
    RefreshForm(){      
      let dt = new Date();
      const companyId = this.currentUser?.CompanyID ?? this.currentUser?.companyId ?? 0;
      const fiscalYearId = this.currentUser?.FiscalYearID ?? this.currentUser?.fiscalYearId;
      this.form = {
        InvoiceNo: 10000,
        InvoiceDate: dt,
        Cashier: `${this.currentUser?.FirstName || ''} ${this.currentUser?.LastName || ''}`.trim(),
        POSCode: "02",
        ModuleID: 'POS',
        CompanyID: companyId,
        InvoiceType: 'INV',
        FiscalYearID: fiscalYearId,
        BranchID: 1
      };
      this.posItemRows= [
        {
          SrNo:1,
          id:"1",
          Description: "",
          ShortDesc: "",
          Amount:0,
          Quantity:1,
          SalePrice:0,
          customCode:"",
          IsInserted: false
        },
        
      ];         
      this.displayItemSearchModal = false;
      this.receiveAmount = 0;
      this.discountPercnt = 0;
      this.posItemSearchKeyUptime = new Date().getTime();
      this.posItemSearchKeyUpPrevTime = 0;
      this.txtTotalAmount = 0;    
    }
    /**
     *  on enter key press in custom code field
     *  */
    onKeydown($event: any, rowData: posItemRow, rowIndex:number){
    debugger;
      // console.log($event);
      // console.log(rowData);
      // console.log(rowIndex);
      this.SelectedIndexOfDtl = rowIndex;
      if(rowData.customCode.length <= 0)    
      {
        this.GetSearchItems("");
        
        this.txtSearch = "";
        this.displayItemSearchModal = true;
      }
      else{
        this.txtSearch = rowData.customCode;
        this.GetSearchItems(rowData.customCode);
        this.displayItemSearchModal = true;
      }

    }

    onKeyupQty($event: any, rowData: posItemRow, rowIndex: number){
      
      rowData.Amount = rowData.Quantity * rowData.SalePrice;
      this.CalculateTotalAmount();
    }

    onBlurQtyAndRate($event: any, rowData: posItemRow, rowIndex: number){
      debugger;
      rowData.Amount = rowData.Quantity * rowData.SalePrice;
      this.CalculateTotalAmount();
      this.UpdateDetailRow(this.form?.InvoiceNo || 0, rowData)
    }


    onDeleteRowClick(rowData: posItemRow, rowIndex: number){
      let len = this.posItemRows.length - 1;
      // don't remove the last row.
      if (len == rowIndex)
      {
        return;
      }
      console.log( rowData );
      console.log( rowIndex );
      
      this.posItemRows.splice(rowIndex, 1);
      this.posItemRows = [...this.posItemRows];

      let dbDtl = this.MapInvoiceDetailItems(rowData, this.form.InvoiceNo!);      
      
      this.posService.DeleteInvDetail(dbDtl).subscribe({
        next: (sr) => {
          console.log(sr.Data);            
        },
        error:(error) =>{
          console.error(error);
          this.messageService.add({severity:'error', summary: error.Title, detail: error.Message, life: 3000});
        }});


    }
    BtnReceiveAmountClick($event: any){
      this.modelTotalAmount = this.txtTotalAmount;
      this.CalculateReceiveAmountValues(null);
      this.displayInvoiceTotalModal = true;
    }
    onPosItemRowSelect($event: any){
      debugger;
      // console.log(this.selectedItemFromSearch);
      // this.SelectedIndexOfDtl
      
      let oldItem = this.posItemRows![this.SelectedIndexOfDtl!];
      let selItem = this.selectedItemFromSearch;
      
      
      // this.posItemRows![this.SelectedIndexOfDtl!] = {
      //   id: oldItem.id,
      //   SrNo: oldItem.SrNo,
      //   customCode: selItem?.CustomCode || "",  //because of this custome code we cannot update item in pos
      //   Description: selItem?.Description || "",
      //   SalePrice: selItem?.SalePrice || 0,
      //   Quantity: oldItem.Quantity,
      //   Amount: selItem?.SalePrice! * oldItem.Quantity,
      //   ItemId: selItem?.ItemId,
      //   IsInserted: oldItem.IsInserted
      // }
      
      this.posItemRows![this.SelectedIndexOfDtl!].customCode = selItem?.CustomCode || "";  //because of this custome code we cannot update item in pos
      this.posItemRows![this.SelectedIndexOfDtl!].Description = selItem?.Description || "";
      this.posItemRows![this.SelectedIndexOfDtl!].SalePrice = selItem?.SalePrice || 0;
      this.posItemRows![this.SelectedIndexOfDtl!].Amount = selItem?.SalePrice! * oldItem.Quantity;
      this.posItemRows![this.SelectedIndexOfDtl!].ItemId = selItem?.ItemId;

  

      this.displayItemSearchModal = false;
      debugger;
      //save data to db.       
      if( this.posItemRows![this.SelectedIndexOfDtl!].IsInserted){
        // update data in db
        this.UpdateDetailRow(this.form?.InvoiceNo || 0, this.posItemRows![this.SelectedIndexOfDtl!])
      }
      else
      {
        if(this.posItemRows!.length <= 1){
          //save master and detail
          this.SaveFirstRow(this.form!, this.posItemRows![this.SelectedIndexOfDtl!]);
        }  
        else{
          // save detail only
          this.SaveDetailRow( this.form!.InvoiceNo, this.posItemRows![this.SelectedIndexOfDtl!]);
        }
        this.AddNewRowToDetail();
      }
      
    }
  
    
    onPosItemSearchKeyUp($event:any){
      // debugger;
      this.posItemSearchKeyUptime = new Date().getTime();
      //get new items after 1.0 sec
      if(this.posItemSearchKeyUptime - this.posItemSearchKeyUpPrevTime! >= 1000){
        this.posItemSearchKeyUpPrevTime = this.posItemSearchKeyUptime;
        this.posItemSearchKeyUptime = new Date().getTime();
        this.GetSearchItems(this.txtSearch!);
      }  
      
    }

    public AddNewRowToDetail(){
      let lastItem = this.posItemRows![this.posItemRows!.length -1]
      
      let newItemRow: posItemRow =  {
        SrNo:lastItem.SrNo + 1,
        id:(lastItem.SrNo + 1).toString(),
        Description: "",
        ShortDesc: "",
        Amount:0,
        Quantity:1,
        SalePrice:0,
        customCode:"",
        IsInserted:false
      }
      this.posItemRows?.push(newItemRow);
      setTimeout(()=>{ // this will make the execution after the above boolean has changed
        
        // hrBeforeDtlRef is the refrence to the hr element above the p-table component 
        //now we are following the path from hr to the required cell to set focus.
        /** this.hrBeforeDtlRef!.nativeElement.nextSibling.nextSibling.firstElementChild
          .firstElementChild.firstElementChild.tBodies[0]
          .rows[this.posItemRows!.length -1].cells[1].click(); */
        
        this.hrBeforeDtlRef!.nativeElement.nextSibling.firstElementChild
          .firstElementChild.firstElementChild.tBodies[0]
          .rows[this.posItemRows!.length -1].cells[1].click();
      },0);  
      this.CalculateTotalAmount();
    }

    CalculateTotalAmount(){
      this.txtTotalAmount = 0;
      for(let row of this.posItemRows!){
        this.txtTotalAmount += row.Amount
      }
    }

    CalculateReceiveAmountValues($event: any){


      this.netAmount =  this.modelTotalAmount! - ( this.modelTotalAmount! * this.discountPercnt! / 100);
      
      this.balanceAmount = this.receiveAmount! - this.netAmount;
    }

    onSaleComplete($event: any){      
      this.displayInvoiceTotalModal = false;
      debugger;
      let pos:POS = {};
      pos.Task = "UPDATE_MASTER";

      //save data to db. 
      let dbMst:InvoiceMaster = {
        InvoiceNo: this.form.InvoiceNo,
        InvoiceDate: this.form.InvoiceDate,
        CompanyID: this.form.CompanyID,
        ModuleID: this.form.ModuleID,
        TotalAmount: this.modelTotalAmount,
        DiscountPercent: this.discountPercnt,
        DiscountAmount: ( this.modelTotalAmount! * this.discountPercnt! / 100),
        SaleTaxPercent: 0,
        SaleTaxAmount: 0,
        NetAmount: this.netAmount,
        ReceivedAmount: this.receiveAmount,
        BalanceAmount: this.balanceAmount,
        InvoiceType: this.form.InvoiceType,
        FiscalYearID: this.form.FiscalYearID,
        OtherTaxPercent: 0,
        OtherTaxAmount: 0,
        CreditLimit: 0,
        ConsumedCredit: 0,
        BalanceCredit: 0,
        Status: 'P',
        BranchID: 1
      }
      pos.objInvoiceMaster = dbMst;

      // this.showLoader = true;
      this.posService.UpdatePosTrans(pos).subscribe({
        next: sr => {
          debugger;
          // this.showLoader = false;
          console.log( sr.Data);
          
          this.RefreshForm();
            
        },
        error:error =>{
        
          console.error(error);
          this.messageService.add({severity:'error', summary: error.Title, detail: error.Message, life: 3000});
        
        }});
    }

    NoItemsFound = true;
    GetSearchItems(pramQuery:string) {
      const companyId = this.form.CompanyID
        || this.currentUser?.CompanyID
        || this.currentUser?.companyId
        || 0;

      this.posService.GetSearchItems({
        Query: pramQuery,
        companyId: companyId
      }).subscribe({
        next: (sr) => {
          const data = sr?.Data;
          this.posItemSearch = Array.isArray(data) ? data : (data?.Items ?? []);
          this.NoItemsFound = this.posItemSearch.length === 0;
        },
        error: (error) => {
          console.error(error);
          this.posItemSearch = [];
          this.NoItemsFound = true;
          const detail = error?.Message || error?.message
            || (typeof error === 'string' ? error : 'Could not load products. Check API is running.');
          this.messageService.add({severity:'error', summary: 'Search failed', detail, life: 5000});
        }
      });
    }
   /**
   * SAVE_MASSTER_WITH_DETAIL
   * @param mst 
   * @param dtl 
   */
    SaveFirstRow(mst:posTrans, dtl:posItemRow){
      let pos:POS = {};
      pos.Task = "SAVE_MASSTER_WITH_DETAIL";
      
      const fiscalYearId = Number(this.form.FiscalYearID || this.currentUser?.FiscalYearID || 1);
      let dbMst:InvoiceMaster = {InvoiceNo:0, InvoiceDate:mst.InvoiceDate, CompanyID:this.form.CompanyID, } ;
      dbMst.CustomerID = 1;
      dbMst.CreateUser = this.currentUser.UserID;
      dbMst.CreateDate = new Date();      
      dbMst.ModuleID = this.form.ModuleID;
      dbMst.TotalAmount = this.txtTotalAmount;
      dbMst.DiscountPercent = 0;
      dbMst.DiscountAmount = 0;
      dbMst.SaleTaxPercent = 0;
      dbMst.SaleTaxAmount = 0;
      dbMst.NetAmount = this.txtTotalAmount;
      dbMst.InvoiceType = this.form.InvoiceType;
      dbMst.FiscalYearID = fiscalYearId;
      dbMst.OtherTaxPercent = 0;
      dbMst.OtherTaxAmount = 0;
      dbMst.ConsumedCredit = 0;
      dbMst.BranchID = this.form.BranchID;      
      dbMst.Status = 'N';
      pos.objInvoiceMaster = dbMst;
      
      
      let dbDtl:InvoiceDetailItems = this.MapInvoiceDetailItems(dtl, 1);
      if (!dbDtl.ItemCode || dbDtl.InvoiceRate! <= 0) {
        this.messageService.add({severity:'warn', summary: 'Invalid item', detail: 'Select a product with a price before saving.', life: 4000});
        return;
      }

      pos.objInvoiceDetailItems = dbDtl;
      // this.showLoader = true;
      let IndexToBeSaved = this.SelectedIndexOfDtl!;
      this.posService.SavePosTrans(pos).subscribe({
        next: (sr) => {
          debugger;
          // this.showLoader = false;
          this.form!.InvoiceNo = sr.Data;
          this.posItemRows![IndexToBeSaved].IsInserted = true;
            
        },
        error:(error) =>{
          console.error(error);
          this.messageService.add({severity:'error', summary: error.Title, detail: error.Message, life: 3000});
        }});
    }
    /**
     * saves the detail row of pos entry, this method is used to save the rows after first detail row has been saved with master row.
     * @param InvoiceNo 
     * @param dtl 
     */
    SaveDetailRow(InvoiceNo: number, dtl: posItemRow) {
      let pos:POS = {};
      pos.Task = "SAVE_DETAIL";
      
      
      
      let dbDtl:InvoiceDetailItems = this.MapInvoiceDetailItems(dtl, InvoiceNo);

      pos.objInvoiceDetailItems = dbDtl;
      let IndexToBeSaved = this.SelectedIndexOfDtl!;
      this.posService.SavePosTrans(pos).subscribe(
        sr => {
          debugger;
          // this.showLoader = false;
          dtl.SrNo = sr.Data;
          this.posItemRows![IndexToBeSaved].IsInserted = true;
            
        },
        error =>{
          console.error(error);
          this.messageService.add({severity:'error', summary: error.Title, detail: error.Message, life: 3000});
        },);
    }
    
    UpdateDetailRow(InvoiceNo: number, dtl: posItemRow) {
      let pos:POS = {};
      pos.Task = "UPDATE_DETAIL";
      
      
      
      let dbDtl:InvoiceDetailItems = this.MapInvoiceDetailItems(dtl, InvoiceNo);

      pos.objInvoiceDetailItems = dbDtl;
      // this.showLoader = true;
      this.posService.UpdatePosTrans(pos).subscribe({
        next: (sr) => {
          debugger;
          // dtl.SrNo = sr.Data;
          // this.showLoader = false;
          // console.log( sr.Data);
            
        },
        error:(error) =>{
          console.error(error);
          this.messageService.add({severity:'error', summary: error.Title, detail: error.Message, life: 3000});
        }
      }
        );
    }
    MapInvoiceDetailItems( dtl:posItemRow, invNo:number):InvoiceDetailItems{
      const fiscalYearId = Number(this.form.FiscalYearID || this.currentUser?.FiscalYearID || 1);
      let dbDtl:InvoiceDetailItems ={InvoiceNo :invNo};
      dbDtl.SrNo = dtl.SrNo;
      dbDtl.ItemCode = dtl.ItemId || dtl.customCode || '';
      dbDtl.ItemDescription = dtl.Description;
      dbDtl.Quantity = dtl.Quantity;
      dbDtl.Unit = 'Nos';
      dbDtl.InvoiceRate = dtl.SalePrice;
      dbDtl.InvoiceValue = dtl.Amount;
      dbDtl.CreateUser = this.currentUser.UserID;
      dbDtl.CreateDate = new Date();
      dbDtl.CompanyID = this.form.CompanyID;
      dbDtl.ModuleID = this.form.ModuleID;
      dbDtl.FiscalYearID = fiscalYearId;
      dbDtl.InvoiceType = this.form.InvoiceType;
      dbDtl.DiscountPercent = 0;
      dbDtl.DiscountAmount = 0;
      dbDtl.BranchID = 1;
      dbDtl.TaxPercent = 0;
      dbDtl.TaxAmount = 0;
      return dbDtl;
    }

    RevMapInvoiceDetailItems( dbDtl:InvoiceDetailItemEditModel):posItemRow{
      debugger;
      let dtl:posItemRow = {
        SrNo: dbDtl.SrNo!,
        id: '',
        customCode: dbDtl.CustomCode!,
        Description: dbDtl.ItemDescription!,
        ShortDesc: "",
        Quantity: dbDtl.Quantity!,
        SalePrice: dbDtl.InvoiceRate!,
        Amount: dbDtl.InvoiceValue!,
        ItemId: dbDtl.ItemCode,
        IsInserted: false
      }
      return dtl;
    }

    onCloseClick(event:any){
      this.router.navigate([`/pos/invoices-list`]);
    }
 }
