export interface Auction {
  id: number;
  title: string;
  description: string;
  startingPrice?: number;
  currentPrice: number;
  startTime: Date;
  endTime: Date;
  status: AuctionStatus;
  /** Relógio lógico do leilão: sobe a cada lance aceito pelo servidor. */
  sequence?: number;
  bidCount: number;
  sellerId?: string;
  sellerName?: string;
  winnerId?: string | null;
  winnerName?: string | null;
  recentBids?: Bid[];
}

export enum AuctionStatus {
  Draft = 0,
  Scheduled = 1,
  Active = 2,
  Ended = 3,
  Cancelled = 4
}

export interface Bid {
  id: number;
  auctionId?: number;
  amount: number;
  placeAt: Date;
  bidderId?: string;
  bidderName?: string;
  sequence?: number;
}

/** Payload do evento "BidPlaced" enviado pelo hub. */
export interface BidPlacedEvent {
  auctionId: number;
  bidId: number;
  amount: number;
  bidderId: string;
  bidderName: string;
  sequence: number;
  timestamp: string;
}

export interface AuctionEndedEvent {
  auctionId: number;
  winnerId: string | null;
  winnerName: string | null;
  finalPrice: number;
}

export interface PresenceEvent {
  auctionId: number;
  viewers: number;
}
