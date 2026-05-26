//1. Import all dependencies
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { HttpClient, HttpHeaders, HttpErrorResponse, HttpParams } from '@angular/common/http';

// import { Http, Response, Request, RequestOptions, Headers } from '@angular/http';
import { Configuration } from '../../app.constants';
import { AuthService } from '../login/auth.service';

import { ServiceResponse } from '../../models/service-response.model';

// import { ChowChoiceRequestOptions } from '../../../app.request-options';

import { UtilService } from '../../services/util.service';
import { POS, InvoiceMasterListing, InvoiceDetailItems, InvoiceMaster } from '../../models/posTrans';

@Injectable({
  providedIn: 'root'
})
export class PosService {


    
   
    
    
    headers = new HttpHeaders().set('Content-Type', 'application/json');

    //4. Passsing the Http dependency to the constructor to access Http functions
  constructor(private http: HttpClient, private _configuration: Configuration, private _utilService: UtilService, 
    private _authService: AuthService) {
       
    }
    private getApiUrl(): string {
    return this._configuration.WebApi;
  }

    GetSearchItems(data: any): Observable<any> {
      let API_URL = `${this.getApiUrl()}/POS/GetSearchItems`;
      return this.http.post(API_URL, data, { headers: this._authService.GetHttpHeaders() })
        .pipe(
          catchError(this._utilService.handleError)
        )
    }

    GetInvoicesList(data: any): Observable<any> {
      const API_URL = `${this.getApiUrl()}/POS/getinvoices`;
      const params = this.buildListParams(data);
      return this.http.get(API_URL, { headers: this._authService.GetHttpHeaders(), params })
        .pipe(
          catchError(this._utilService.handleError)
        )
    }
  
    SavePosTrans(data:POS): Observable<any> {
      let API_URL = `${this.getApiUrl()}/POS`;
      return this.http.post(API_URL, data, { headers: this._authService.GetHttpHeaders() })
        .pipe(
          catchError(this._utilService.handleError)
        )
    }
    
    UpdatePosTrans(data:POS): Observable<any> {
      let API_URL = `${this.getApiUrl()}/POS`;
      return this.http.put(API_URL, data, { headers: this._authService.GetHttpHeaders() }).pipe(
        catchError(this._utilService.handleError)
      )      
    }
    
    DeleteInvDetail(data:InvoiceDetailItems): Observable<any> {
      let API_URL = `${this.getApiUrl()}/POS/DeleteInvDetail`;
      return this.http.post(API_URL, data, { headers: this._authService.GetHttpHeaders() }).pipe(
        catchError(this._utilService.handleError)
      )      
    }

    /**
     * Delete whole invoice including all details if it is not posted.
     * @param data Invoice to delete
     * @returns any
     */
    DeleteInvoice(data:InvoiceMaster): Observable<any> {
      let API_URL = `${this.getApiUrl()}/POS`;
      return this.http.delete(API_URL, { headers: this._authService.GetHttpHeaders(), body: data }).pipe(
        catchError(this._utilService.handleError)
      )      
    }
    /**
     * Delete a list of whole invoices including all details if it is not posted.
     * @param data Invoices to delete
     * @returns any
     */
     DeleteInvoicesList(data:InvoiceMaster[]): Observable<any> {
      let API_URL = `${this.getApiUrl()}/POS/DeleteList`;
      return this.http.delete(API_URL, { headers: this._authService.GetHttpHeaders(), body: data }).pipe(
        catchError(this._utilService.handleError)
      )      
    }
    
    GetInvoiceDetails(data: InvoiceMasterListing): Observable<any> {
      const API_URL = `${this.getApiUrl()}/POS/getinvoicedetails`;
      const params = new HttpParams()
        .set('invoiceNo', String(data?.InvoiceNo ?? ''))
        .set('invoiceType', data?.InvoiceType ?? '')
        .set('fiscalYearId', String(data?.FiscalYearID ?? 0))
        .set('companyId', String(data?.CompanyID ?? 0));
      return this.http.get(API_URL, { headers: this._authService.GetHttpHeaders(), params })
        .pipe(
          catchError(this._utilService.handleError)
        )
    }

    private buildListParams(data: any): HttpParams {
      return new HttpParams()
        .set('query', data?.query ?? '')
        .set('companyId', String(data?.companyId ?? 0))
        .set('limit', String(data?.limit ?? 0))
        .set('offset', String(data?.offset ?? 0));
    }



    // Create
    createTask(data: any): Observable<any> {
      let API_URL = `${this.getApiUrl()}/create-task`;
      return this.http.post(API_URL, data, { headers: this._authService.GetHttpHeaders() })
        .pipe(
          catchError(this._utilService.handleError)
        )
    }
  
    // Read
    showTasks() {
      return this.http.get(`${this.getApiUrl()}`);
    }
  
    // Update
    updateTask(id: any, data: any): Observable<any> {
      let API_URL = `${this.getApiUrl()}/update-task/${id}`;
      return this.http.put(API_URL, data, { headers: this._authService.GetHttpHeaders() }).pipe(
        catchError(this._utilService.handleError)
      )
    }
  
    // Delete
    deleteTask(id: any): Observable<any> {
      var API_URL = `${this.getApiUrl()}/delete-task/${id}`;
      return this.http.delete(API_URL, { headers: this._authService.GetHttpHeaders() }).pipe(
        catchError(this._utilService.handleError)
      )
    }
}
