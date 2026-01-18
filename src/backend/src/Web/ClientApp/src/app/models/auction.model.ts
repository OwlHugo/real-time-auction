export interface Auction {
  id: number;
  title: string;
  description: string;
  currentPrice: number;
  startTime: Date;
  endTime: Date;
  status: AuctionStatus;
  bidCount: number;
  seller?: string;
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
}
