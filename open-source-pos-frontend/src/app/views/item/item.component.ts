import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';

import { posItem } from '../../models/posTrans';

import { ConfirmationService } from 'primeng/api';
import { MessageService } from 'primeng/api';
import { ItemService } from './item.service';
import { AuthService } from '../login/auth.service';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';

@Component({
  selector: 'app-item',
  templateUrl: './item.component.html',
  styleUrls: ['./item.component.scss'],
  styles: [`
        :host ::ng-deep .p-dialog .product-image {
            width: 150px;
            margin: 0 auto 2rem auto;
            display: block;
        }
    `],
    providers: [MessageService,ConfirmationService]
})
export class ItemComponent implements OnInit {



  public itemFormValidated: boolean = false;
  public isRequestProcessing: boolean = false;
  public isFormSubmitted: boolean = false;
  /**Is the item is being updated or created */
  isEditing:boolean = false;



  productDialog!: boolean;  

  items!: posItem[];
  totalItems!: number;  

  item!: posItem;
  selectedItems!: posItem[];  
  currentUser:any;

  @ViewChild('posItemForm') posItemForm?: NgForm;

  constructor(private messageService: MessageService, 
    private confirmationService: ConfirmationService, private itemService: ItemService,
    private authService: AuthService, private activatedroute: ActivatedRoute) {
      this.currentUser = this.authService.GetlocalStorageUser();
    }

  ngOnInit() {
      let data = this.activatedroute.snapshot.routeConfig?.path;

      if (!this.currentUser?.Token) {
        this.messageService.add({severity:'error', summary: 'Not logged in', detail: 'Please log in again.', life: 5000});
        return;
      }

      const companyId = this.currentUser.CompanyID ?? this.currentUser.companyId ?? 0;
      let params = {
        query: '',
        companyId: companyId,
        limit: 0,
        offset: 0
      };
      this.itemService.GetItems(params).subscribe({
        next: (sr) => {
          debugger;
            
          this.items = sr.Data.Items;
          this.totalItems = sr.Data.Count;          
            
        },
        error:(error) =>{
          debugger;          
          console.error(error);
          this.messageService.add({severity:'error', summary: 'Error Loading Items!', detail: error, life: 3000});
        }});

        if(data && data == "items/new"){
          this.openNew();
        }
      
  }

  openNew() {
    this.isEditing = false;
      this.item = {
        CustomCode:"",
        Description:"",
        ShortDesc: "",
        Id:"0",                
      };
      
      this.productDialog = true;
  }

  deleteSelectedItems() {
      if (!this.selectedItems?.length) {
        return;
      }
      this.confirmationService.confirm({
          message: `Delete ${this.selectedItems.length} selected item(s)?`,
          header: 'Confirm delete',
          icon: 'pi pi-exclamation-triangle',
          accept: () => {
            const companyId = this.currentUser.CompanyID ?? this.currentUser.companyId ?? 0;
            this.selectedItems.forEach(item => this.removeItem(item, companyId, false));
          }
      });
  }

  
  editProduct(item: posItem) {
      this.item = {...item};
      this.productDialog = true;
      this.isEditing = true;
  }

  deleteProduct(item: posItem) {
      this.confirmationService.confirm({
          message: 'Delete ' + item.Description + '?',
          header: 'Confirm delete',
          icon: 'pi pi-exclamation-triangle',
          accept: () => {
            const companyId = this.currentUser.CompanyID ?? this.currentUser.companyId ?? 0;
            this.removeItem(item, companyId, true);
          }
      });
  }

  private removeItem(item: posItem, companyId: number, showToast: boolean) {
    if (!item.ItemId) {
      return;
    }
    this.itemService.DeleteItem(item.ItemId, companyId).subscribe({
      next: (sr) => {
        if (sr?.IsValid === false) {
          this.messageService.add({severity:'error', summary: sr.Title || 'Delete failed', detail: sr.Message, life: 6000});
          return;
        }
        this.items = this.items.filter(i => i.ItemId !== item.ItemId);
        this.totalItems = this.items.length;
        if (showToast) {
          this.messageService.add({severity:'success', summary: 'Deleted', detail: item.Description, life: 3000});
        }
      },
      error: (error) => {
        const detail = error?.Message || error?.message || (typeof error === 'string' ? error : 'Delete failed.');
        this.messageService.add({severity:'error', summary: 'Delete failed', detail, life: 6000});
      }
    });
  }

  hideDialog() {
      this.productDialog = false;
      
  }

  /** Build API payload with required PascalCase fields (matches .NET PosItem). */
  private buildItemPayload(forUpdate: boolean): posItem {
    const companyId = this.currentUser?.CompanyID ?? this.currentUser?.companyId ?? 0;
    return {
      ItemId: String(this.item.ItemId ?? this.item.Id ?? ''),
      Id: String(this.item.Id ?? this.item.ItemId ?? '0'),
      CustomCode: (this.item.CustomCode ?? '').trim(),
      Description: (this.item.Description ?? '').trim(),
      ShortDesc: (this.item.ShortDesc ?? '').trim(),
      SalePrice: Number(this.item.SalePrice) || 0,
      CompanyID: companyId,
      CreateUser: forUpdate ? (this.item.CreateUser ?? this.currentUser?.UserID) : this.currentUser?.UserID,
      UpdateUser: forUpdate ? this.currentUser?.UserID : undefined,
    };
  }

  private formatApiError(error: any): string {
    if (error?.Message) return error.Message;
    if (error?.Errors) {
      return Object.values(error.Errors).flat().join('\n');
    }
    if (typeof error === 'string') return error;
    return 'Request failed.';
  }

  SaveItem() {
    if (!this.posItemForm?.valid) {
      this.itemFormValidated = true;
      return;
    }

    this.itemFormValidated = true;
    this.isFormSubmitted = true;
    this.isRequestProcessing = true;

    if (this.isEditing) {
      const payload = this.buildItemPayload(true);
      this.itemService.UpdateItem(payload).subscribe({
        next: (sr) => {
          this.isRequestProcessing = false;
          this.itemFormValidated = false;
          if (sr?.IsValid === false) {
            this.messageService.add({severity:'error', summary: sr.Title || 'Update failed', detail: this.formatApiError(sr), life: 6000});
            return;
          }
          const idx = this.findIndexById(payload.ItemId!);
          if (idx >= 0) {
            this.items[idx] = { ...this.items[idx], ...payload };
          }
          this.messageService.add({severity:'success', summary: 'Successful', detail: 'Product Updated', life: 3000});
          this.productDialog = false;
          this.item = { CustomCode: '', Description: '', ShortDesc: '', Id: '0' };
          this.items = [...this.items];
        },
        error: (error) => {
          this.isRequestProcessing = false;
          console.error(error);
          this.messageService.add({severity:'error', summary: 'Update failed', detail: this.formatApiError(error), life: 6000});
        }
      });
    } else {
      const payload = this.buildItemPayload(false);
      payload.CreateUser = this.currentUser.UserID;
      this.itemService.SaveItem(payload).subscribe({
        next: (sr) => {
          this.isRequestProcessing = false;
          this.itemFormValidated = false;
          if (sr?.IsValid === false) {
            this.messageService.add({severity:'error', summary: sr.Title || 'Create failed', detail: this.formatApiError(sr), life: 6000});
            return;
          }
          payload.ItemId = String(sr.Data);
          this.items.push(payload);
          this.messageService.add({severity:'success', summary: 'Successful', detail: 'Product Created', life: 3000});
          this.productDialog = false;
          this.item = { CustomCode: '', Description: '', ShortDesc: '', Id: '0' };
          this.items = [...this.items];
        },
        error: (error) => {
          this.isRequestProcessing = false;
          console.error(error);
          this.messageService.add({severity:'error', summary: 'Create failed', detail: this.formatApiError(error), life: 6000});
        }
      });
    }
    
    
    
}  

  findIndexById(id: string): number {
      let index = -1;
      for (let i = 0; i < this.items.length; i++) {
          if (this.items[i].ItemId === id) {
              index = i;
              break;
          }
      }

      return index;
  }
}
