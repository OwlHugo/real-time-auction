import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Auction } from '../models/auction.model';

@Injectable({
  providedIn: 'root'
})
export class AuctionService {
  private apiUrl = '/api/auctions';

  constructor(private http: HttpClient) { }

  getAuctions(status?: number, page: number = 1, pageSize: number = 10): Observable<Auction[]> {
    let params = new HttpParams()
      .set('Page', page.toString())
      .set('PageSize', pageSize.toString())
      .set('SortBy', 'created')
      .set('SortDescending', 'true');

    if (status !== undefined) {
      params = params.set('Status', status.toString());
    }

    return this.http.get<any>(this.apiUrl, { params }).pipe(
      map(response => response.items || [])
    );
  }

  getAuction(id: number): Observable<Auction> {
    return this.http.get<Auction>(`${this.apiUrl}/${id}`);
  }

  createAuction(auction: Partial<Auction>): Observable<any> {
    return this.http.post(this.apiUrl, auction, { withCredentials: true });
  }

  placeBid(auctionId: number, amount: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${auctionId}/bids`, { amount }, { withCredentials: true });
  }
}
