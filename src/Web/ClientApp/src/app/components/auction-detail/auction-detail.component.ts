import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { Auction } from '../../models/auction.model';
import { AuctionService } from '../../services/auction.service';
import { AuthService } from '../../services/auth.service';
import { SignalRService } from '../../services/signalr.service';

@Component({
  selector: 'app-auction-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './auction-detail.component.html',
})
export class AuctionDetailComponent implements OnInit, OnDestroy {
  auction: Auction | null = null;
  bidAmount: number = 0;
  timeLeft: string = '00:00:00';
  isLowTime: boolean = false;
  showOutbidAlert: boolean = false;
  currentUserId: string | null = null;
  isLoggedIn: boolean = false;
  bidError: string | null = null;
  isSubmittingBid: boolean = false;

  private subscription: Subscription | null = null;
  private timerSubscription: Subscription | null = null;
  private bidSound = new Audio('https://assets.mixkit.co/active_storage/sfx/2568/2568-preview.mp3');

  constructor(
    private route: ActivatedRoute,
    private auctionService: AuctionService,
    private signalRService: SignalRService,
    private authService: AuthService
  ) { }

  async ngOnInit(): Promise<void> {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.authService.isLoggedIn$.subscribe(status => {
      this.isLoggedIn = status;
    });

    this.authService.currentUser$.subscribe(user => {
      this.currentUserId = user?.id || null;
    });

    this.auctionService.getAuction(id).subscribe(auction => {
      this.auction = auction;
      this.bidAmount = auction.currentPrice + 10;
      this.startCountdown();
    });

    await this.signalRService.startConnection();
    await this.signalRService.joinAuction(id);

    this.subscription = this.signalRService.bidPlaced$.subscribe(bid => {
      if (bid && bid.auctionId === id) {
        if (this.auction) {
          this.bidSound.play().catch(() => { });

          if (this.currentUserId && bid.bidderId !== this.currentUserId) {
            const lastBidderId = this.auction.recentBids && this.auction.recentBids.length > 0
              ? this.auction.recentBids[0].bidderId
              : null;

            if (lastBidderId === this.currentUserId) {
              this.showOutbidAlert = true;
              setTimeout(() => this.showOutbidAlert = false, 5000);
            }
          }

          this.auction.currentPrice = bid.amount;
          this.auction.bidCount++;
          this.bidAmount = bid.amount + 10;

          if (!this.auction.recentBids) this.auction.recentBids = [];
          this.auction.recentBids = [{
            id: bid.bidId,
            amount: bid.amount,
            placeAt: new Date(bid.timestamp),
            bidderId: bid.bidderId
          }, ...this.auction.recentBids];
        }
      }
    });

    this.signalRService.onEvent('AuctionStarted', (data) => {
      if (this.auction && data.auctionId === id) {
        this.auction.status = 2;
      }
    });

    this.signalRService.onEvent('AuctionEnded', (data) => {
      if (this.auction && data.auctionId === id) {
        this.auction.status = 3;
        this.timerSubscription?.unsubscribe();
      }
    });
  }

  private startCountdown(): void {
    if (this.timerSubscription) this.timerSubscription.unsubscribe();

    this.timerSubscription = interval(1000).subscribe(() => {
      if (this.auction) {
        const now = new Date().getTime();
        const end = new Date(this.auction.endTime).getTime();
        const diff = end - now;

        if (diff <= 0) {
          this.timeLeft = 'Encerrado';
          this.isLowTime = false;
          this.timerSubscription?.unsubscribe();
          return;
        }

        const hours = Math.floor(diff / (1000 * 60 * 60));
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((diff % (1000 * 60)) / 1000);

        this.timeLeft = `${this.pad(hours)}:${this.pad(minutes)}:${this.pad(seconds)}`;
        this.isLowTime = diff < 60000;
      }
    });
  }

  private pad(num: number): string {
    return num.toString().padStart(2, '0');
  }

  placeBid(): void {
    if (this.auction && this.bidAmount > this.auction.currentPrice) {
      const amountSubmitted = this.bidAmount;
      this.isSubmittingBid = true;
      this.bidError = null;

      this.auctionService.placeBid(this.auction.id, amountSubmitted).subscribe({
        next: () => {
          this.isSubmittingBid = false;
          this.showOutbidAlert = false;

          // Fallback: Se o SignalR falhar, atualizamos o preço localmente
          if (this.auction && this.auction.currentPrice < amountSubmitted) {
            this.auction.currentPrice = amountSubmitted;
            this.bidAmount = amountSubmitted + 10;
          }
        },
        error: (err) => {
          this.isSubmittingBid = false;
          console.error('Failed to place bid', err);

          // Tenta pegar a mensagem detalhada do backend
          this.bidError = err.error?.detail || err.error?.title || 'Erro ao enviar lance. Verifique o valor e o tempo.';

          // Se o erro for que o lance já foi superado, atualizamos o valor sugerido
          if (this.bidError.includes('maior que o preço atual')) {
            // Opcional: recarregar o leilão para sincronizar
            this.loadAuction();
          }

          setTimeout(() => this.bidError = null, 5000);
        }
      });
    }
  }

  private loadAuction(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.auctionService.getAuction(id).subscribe(auction => {
      this.auction = auction;
      if (this.bidAmount <= auction.currentPrice) {
        this.bidAmount = auction.currentPrice + 10;
      }
      this.startCountdown();
    });
  }

  ngOnDestroy(): void {
    if (this.auction) {
      this.signalRService.leaveAuction(this.auction.id);
    }
    this.subscription?.unsubscribe();
    this.timerSubscription?.unsubscribe();
  }
}
