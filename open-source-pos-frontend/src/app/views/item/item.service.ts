//1. Import all dependencies
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';

// import { Http, Response, Request, RequestOptions, Headers } from '@angular/http';
import { Configuration } from '../../app.constants';
import { AuthService } from '../login/auth.service';

import { ServiceResponse } from '../../models/service-response.model';

// import { ChowChoiceRequestOptions } from '../../../app.request-options';

import { UtilService } from '../../services/util.service';
import { posItem, POS } from '../../models/posTrans';

@Injectable({
  providedIn: 'root'
})
export class ItemService {
  
    
    
  headers = new HttpHeaders().set('Content-Type', 'application/json');

  //4. Passsing the Http dependency to the constructor to access Http functions
  constructor(private http: HttpClient, private _configuration: Configuration, private _utilService: UtilService, 
    private _authService: AuthService){}
    private getApiUrl(): string {
    return `${this._configuration.WebApi}/item`;
  }
  GetItems(data: any): Observable<any> {
    debugger;
    let API_URL = `${this.getApiUrl()}/getitems`;
    return this.http.post(API_URL, data, { headers: this._authService.GetHttpHeaders() })
      .pipe(
        catchError(this.error) //this._utilService.handleError
      )
  }

  SaveItem(data:posItem): Observable<any> {    
    debugger;
    return this.http.post(this.getApiUrl(), data, { headers: this._authService.GetHttpHeaders() })
      .pipe(
        catchError(this.error)  //this._utilService.handleError
      )
  }
  
  UpdateItem(data:posItem): Observable<any> {
    return this.http.put(this.getApiUrl(), data, { headers: this._authService.GetHttpHeaders() }).pipe(
      catchError(this.error)
    )      
  }

  DeleteItem(itemId: string, companyId: number): Observable<any> {
    const url = `${this.getApiUrl()}/${itemId}?companyId=${companyId}`;
    return this.http.delete(url, { headers: this._authService.GetHttpHeaders() }).pipe(
      catchError(this.error)
    );
  }
  
    // Handle API errors (500 may return a plain string message from the server)
    error(error: HttpErrorResponse) {
      let errorMessage = '';
      const body = error.error;

      if (body && typeof body === 'object' && body.Errors) {
        for (const property in body.Errors) {
          body.Errors[property].forEach((err: string) => {
            errorMessage += err + ' \n ';
          });
        }
      } else if (body instanceof ErrorEvent) {
        errorMessage = body.message;
      } else if (typeof body === 'string') {
        errorMessage = body;
      } else if (body?.Message) {
        errorMessage = body.Message;
      } else if (body?.message) {
        errorMessage = body.message;
      } else {
        errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      }
      console.log(errorMessage);
      return throwError(() => errorMessage);
    }
}