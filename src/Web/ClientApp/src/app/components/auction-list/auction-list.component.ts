import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Auction } from '../../models/auction.model';
import { AuctionService } from '../../services/auction.service';

@Component({
  selector: 'app-auction-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './auction-list.component.html',
})
export class AuctionListComponent implements OnInit {
  auctions: Auction[] = [];
  loading = true;

  constructor(private auctionService: AuctionService) { }

  ngOnInit(): void {
    this.loading = true;
    this.auctionService.getAuctions().subscribe({
      next: (data) => {
        this.auctions = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching auctions', err);
        this.loading = false;
      }
    });
  }
}
