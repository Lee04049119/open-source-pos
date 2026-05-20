import { Injectable, Inject } from '@angular/core';
import { WINDOW } from './window.provider';
import { environment } from '../environments/environment';
import { RuntimeConfigService } from './services/runtime-config.service';


@Injectable()
export class Configuration {
  url: string = '';
  public domain: string;

  public Server = '';  
  public FileServer = '';

  /**
   * This Property contaion the url for geeting images (Seperate images server) 
   */
  public ImageServerUrl = '';
  /**
   * Main Backend Api URL is contained by this Property  
   */
  public WebApi = '';

  constructor(
    @Inject(WINDOW) private window: any,
    private runtime: RuntimeConfigService,
  ) {
    let domain = this.window.location.hostname;
    let port = this.window.location.port;
    let protocol = this.window.location.protocol;
    
    this.domain = domain;

    const prod = environment.production;
    const runtimeApi = prod
      ? (this.runtime.apiBaseUrlHttps?.trim() || this.runtime.apiBaseUrl?.trim())
      : (this.runtime.apiBaseUrl?.trim() || this.runtime.apiBaseUrlHttps?.trim());
    const runtimeImg = prod
      ? (this.runtime.imageServerUrlHttps?.trim() || this.runtime.imageServerUrl?.trim())
      : (this.runtime.imageServerUrl?.trim() || this.runtime.imageServerUrlHttps?.trim());

    if (runtimeApi) {
      this.WebApi = runtimeApi.replace(/\/+$/, '');
      const defaultImg = prod ? 'https://localhost:9096/' : 'http://localhost:9096/';
      this.ImageServerUrl = (runtimeImg ?? defaultImg).replace(/\/?$/, '/');
      return;
    }

    // Use environment localhost API only when the UI is also on localhost (F5 on LAN IP must hit LAN API).
    const fixedApi = environment.apiBaseUrl?.trim();
    const isLocalUi = domain === 'localhost' || domain === '127.0.0.1';
    if (fixedApi && isLocalUi) {
      this.WebApi = fixedApi.replace(/\/+$/, '');
      this.ImageServerUrl = (environment.imageServerUrl ?? 'http://localhost:9096/').replace(/\/?$/, '/');
      return;
    }

    this.Server = 'https://localhost:44390/';
    this.FileServer = 'https://localhost:44378/';
    // Do not Change anything here it will determin automatically


    if (domain === 'open-source-pos.alishah.pro') {
      let protocal: string = this.window.location.protocol;
      
      this.WebApi = `${protocal}//open-source-pos.alishah.pro/api/api`;
      this.ImageServerUrl = 'https://open-source-pos.alishah.pro/api/api/';
    }
    else if (domain === 'localhost') {
      if (port === '82') { // for spain server
        this.WebApi = `http://localhost:82/api`;
        this.ImageServerUrl = 'http://localhost:9096/';
      }
      else if (port === '') { // for alishan pc
        this.WebApi = `http://localhost/api`;
        this.ImageServerUrl = 'http://localhost:9096/';
      }
      else {
        // Angular dev server (4200) → API on HTTP port 5000
        this.WebApi = `http://localhost:5000/api`;
        this.ImageServerUrl = 'http://localhost:9096/';
      }

    }
    else {
        // LAN / other host: API on same machine, port 5000 (http) or 5001 (https)
        const apiPort = protocol === 'https:' ? '5001' : '5000';
        const apiProtocol = protocol === 'https:' ? 'https:' : 'http:';
        this.WebApi = `${apiProtocol}//${domain}:${apiPort}/api`;
        this.ImageServerUrl = `${apiProtocol}//${domain}:9096/`;
    }


  }



  /** 
   *  Date Formate Used in application used in places where only date is required.
   *  currently this setting is "MM/dd/yyyy" same as (01/25/2019)
   */
  static readonly DateFormate: string = 'dd-MM-yyyy';
  /** 
   *  Time Formate Used in application used in places where only Time is required.
   *  currently this setting is "shortTime" same as "h:mm a" (9:05 AM)
   */
  static readonly TimeFormate: string = 'shortTime';
  /** 
   *  Date and Time Formate Used in application used in places where both Date and Time are required.     
   *  currently this setting is "medium" same as "MMM d, y h:mm:ss a" (Jan 5, 2016 9:05:05 AM)
   */
  static readonly DateTimeFormate: string = 'medium';


}

/**
 * Sample enum constant
 */
export enum PayTypeEnum {

  BankAccount = 47,
  CreditCard = 48,
  DebitCard = 49,
  Cash = 50
}




