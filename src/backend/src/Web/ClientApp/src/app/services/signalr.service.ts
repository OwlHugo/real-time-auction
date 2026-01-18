import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection!: signalR.HubConnection;
  private bidPlacedSubject = new BehaviorSubject<any>(null);

  public bidPlaced$ = this.bidPlacedSubject.asObservable();

  constructor() { }

  public startConnection(): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/auction')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('BidPlaced', (data) => {
      this.bidPlacedSubject.next(data);
    });

    return this.hubConnection.start()
      .then(() => console.log('SignalR Connected'))
      .catch(err => console.error('Error while starting SignalR connection: ' + err));
  }

  public joinAuction(auctionId: number): Promise<void> {
    return this.hubConnection.invoke('JoinAuction', auctionId);
  }

  public onEvent(eventName: string, action: (data: any) => void): void {
    this.hubConnection.on(eventName, action);
  }

  public leaveAuction(auctionId: number): Promise<void> {
    return this.hubConnection.invoke('LeaveAuction', auctionId);
  }

  public stopConnection(): void {
    this.hubConnection?.stop();
  }
}
