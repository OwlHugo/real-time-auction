import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { Auction, AuctionEndedEvent, BidPlacedEvent } from '../../models/auction.model';
import { AuctionService, newIdempotencyKey } from '../../services/auction.service';
import { AuthService } from '../../services/auth.service';
import { LiveState, SignalRService } from '../../services/signalr.service';

/** Mesmo incremento mínimo validado no servidor (PlaceBidCommandHandler.MinimumIncrement). */
const MIN_INCREMENT = 10;
const AUCTION_OPEN = 2;
const AUCTION_ENDED = 3;

@Component({
  selector: 'app-auction-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './auction-detail.component.html',
})
export class AuctionDetailComponent implements OnInit, OnDestroy {
  auction: Auction | null = null;
  bidAmount = 0;
  timeLeft = '00:00:00';
  isLowTime = false;
  showOutbidAlert = false;
  currentUserId: string | null = null;
  isLoggedIn = false;
  bidError: string | null = null;
  notice: string | null = null;
  isSubmittingBid = false;
  viewers = 0;
  liveState: LiveState = 'offline';

  private auctionId = 0;
  /** Último número de sequência aplicado. Lances com sequência menor ou igual são ignorados. */
  private lastSequence = 0;
  private topBidderId: string | null = null;
  private subs: Subscription[] = [];
  private timerSubscription: Subscription | null = null;
  private errorTimer: ReturnType<typeof setTimeout> | null = null;
  private noticeTimer: ReturnType<typeof setTimeout> | null = null;
  private outbidTimer: ReturnType<typeof setTimeout> | null = null;
  private bidSound = new Audio('https://assets.mixkit.co/active_storage/sfx/2568/2568-preview.mp3');

  constructor(
    private route: ActivatedRoute,
    private auctionService: AuctionService,
    private signalRService: SignalRService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.auctionId = Number(this.route.snapshot.paramMap.get('id'));

    this.subs.push(
      this.authService.isLoggedIn$.subscribe(status => this.isLoggedIn = status),
      this.authService.currentUser$.subscribe(user => this.currentUserId = user?.id || null),
      this.signalRService.bidPlaced$.subscribe(bid => this.onBidPlaced(bid)),
      this.signalRService.bidRejected$.subscribe(() =>
        this.flashNotice('Um lance chegou no mesmo instante e foi recusado. Confira o preço atual.')
      ),
      this.signalRService.auctionStarted$.subscribe(e => {
        if (this.auction && e.auctionId === this.auctionId) this.auction.status = AUCTION_OPEN;
      }),
      this.signalRService.auctionEnded$.subscribe(e => this.onAuctionEnded(e)),
      this.signalRService.presence$.subscribe(p => {
        if (p.auctionId === this.auctionId) this.viewers = p.viewers;
      }),
      this.signalRService.state$.subscribe(state => this.onLiveStateChanged(state))
    );

    this.loadAuction();
    this.signalRService.joinAuction(this.auctionId).catch(err =>
      console.error('Sem conexão em tempo real; a página continua funcionando com atualizações manuais.', err)
    );
  }

  get canBid(): boolean {
    return !!this.auction
      && this.auction.status === AUCTION_OPEN
      && !this.isSubmittingBid
      && this.bidAmount >= this.auction.currentPrice + MIN_INCREMENT;
  }

  /** Busca o estado do servidor e reinicia o relógio de sequência. */
  private loadAuction(): void {
    this.auctionService.getAuction(this.auctionId).subscribe(auction => {
      this.auction = auction;
      this.lastSequence = auction.sequence ?? 0;
      this.topBidderId = auction.recentBids?.[0]?.bidderId ?? null;
      this.bidAmount = auction.currentPrice + MIN_INCREMENT;
      this.startCountdown();
    });
  }

  private onLiveStateChanged(state: LiveState): void {
    const wasDown = this.liveState === 'reconnecting' || this.liveState === 'offline';
    this.liveState = state;
    // Voltou depois de cair: pode ter perdido lances enquanto offline, então ressincroniza.
    if (state === 'live' && wasDown && this.auction) {
      this.loadAuction();
    }
  }

  /**
   * Aplica um lance vindo do SignalR OU da resposta do próprio POST (o que chegar primeiro).
   * A sequência torna a operação idempotente: o mesmo lance aplicado duas vezes não muda nada.
   */
  private onBidPlaced(bid: BidPlacedEvent): void {
    if (!this.auction || bid.auctionId !== this.auctionId) return;
    if (bid.sequence <= this.lastSequence) return;

    if (bid.sequence > this.lastSequence + 1) {
      // Faltou uma mensagem no meio: não dá para confiar no preço local, busca o estado oficial.
      this.loadAuction();
      return;
    }

    const isMine = bid.bidderId === this.currentUserId;
    const wasLeading = !!this.currentUserId && this.topBidderId === this.currentUserId;

    this.lastSequence = bid.sequence;
    this.topBidderId = bid.bidderId;
    this.auction.currentPrice = bid.amount;
    this.auction.sequence = bid.sequence;
    this.auction.bidCount = bid.sequence;
    this.auction.recentBids = [
      {
        id: bid.bidId,
        amount: bid.amount,
        placeAt: new Date(bid.timestamp),
        bidderId: bid.bidderId,
        bidderName: bid.bidderName,
        sequence: bid.sequence,
      },
      ...(this.auction.recentBids ?? []).filter(b => b.id !== bid.bidId),
    ].slice(0, 10);

    if (this.bidAmount < bid.amount + MIN_INCREMENT) {
      this.bidAmount = bid.amount + MIN_INCREMENT;
    }

    if (!isMine) {
      this.bidSound.play().catch(() => {});
      if (wasLeading) this.flashOutbid();
    }
  }

  private onAuctionEnded(e: AuctionEndedEvent): void {
    if (!this.auction || e.auctionId !== this.auctionId) return;
    this.auction.status = AUCTION_ENDED;
    this.auction.winnerId = e.winnerId;
    this.auction.winnerName = e.winnerName;
    this.auction.currentPrice = e.finalPrice;
    this.timerSubscription?.unsubscribe();
    this.timeLeft = 'Encerrado';
  }

  private flashOutbid(): void {
    this.showOutbidAlert = true;
    if (this.outbidTimer) clearTimeout(this.outbidTimer);
    this.outbidTimer = setTimeout(() => (this.showOutbidAlert = false), 5000);
  }

  private flashNotice(message: string): void {
    this.notice = message;
    if (this.noticeTimer) clearTimeout(this.noticeTimer);
    this.noticeTimer = setTimeout(() => (this.notice = null), 5000);
  }

  private startCountdown(): void {
    this.timerSubscription?.unsubscribe();

    this.timerSubscription = interval(1000).subscribe(() => {
      if (!this.auction) return;

      const diff = new Date(this.auction.endTime).getTime() - Date.now();
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
    });
  }

  private pad(num: number): string {
    return num.toString().padStart(2, '0');
  }

  placeBid(): void {
    if (!this.auction || !this.canBid) return;

    const amount = Number(this.bidAmount);
    this.isSubmittingBid = true;
    this.bidError = null;

    // Uma chave por clique: se a rede cair e o botão for usado de novo, o servidor não cria dois lances.
    const key = newIdempotencyKey();

    this.auctionService.placeBid(this.auction.id, amount, key).subscribe({
      next: (res) => {
        this.isSubmittingBid = false;
        this.onBidPlaced({
          auctionId: this.auctionId,
          bidId: res.id,
          amount: res.amount,
          bidderId: this.currentUserId ?? '',
          bidderName: 'Você',
          sequence: res.sequence,
          timestamp: res.placedAt,
        });
      },
      error: (err) => {
        this.isSubmittingBid = false;
        console.error('Falha ao enviar lance', err);
        this.showError(err.error?.detail || err.error?.title || 'Erro ao enviar lance. Verifique o valor e o tempo.');
        // Conflito ou valor abaixo do mínimo: o preço mudou, então busca o estado atual.
        if (err.status === 409 || err.status === 400) {
          this.loadAuction();
        }
      }
    });
  }

  private showError(message: string): void {
    this.bidError = message;
    if (this.errorTimer) clearTimeout(this.errorTimer);
    this.errorTimer = setTimeout(() => (this.bidError = null), 5000);
  }

  ngOnDestroy(): void {
    this.signalRService.leaveAuction(this.auctionId).catch(() => {});
    this.subs.forEach(s => s.unsubscribe());
    this.timerSubscription?.unsubscribe();
    [this.errorTimer, this.noticeTimer, this.outbidTimer].forEach(t => t && clearTimeout(t));
  }
}
