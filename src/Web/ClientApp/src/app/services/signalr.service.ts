import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import {
  AuctionEndedEvent,
  BidPlacedEvent,
  PresenceEvent
} from '../models/auction.model';

export type LiveState = 'offline' | 'connecting' | 'live' | 'reconnecting';

/**
 * Uma única conexão SignalR por aba, compartilhada por todas as telas.
 * Os handlers são registrados UMA vez (antes, cada visita à página criava outra conexão
 * e o mesmo lance chegava duplicado). Ao reconectar, a instância recebe um novo
 * connectionId, então entramos de novo em todos os leilões que estavam acompanhados.
 */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private watched = new Set<number>();

  private bidPlacedSubject = new Subject<BidPlacedEvent>();
  private bidRejectedSubject = new Subject<{ auctionId: number; reason: string }>();
  private auctionStartedSubject = new Subject<{ auctionId: number }>();
  private auctionEndedSubject = new Subject<AuctionEndedEvent>();
  private presenceSubject = new Subject<PresenceEvent>();
  private stateSubject = new BehaviorSubject<LiveState>('offline');

  readonly bidPlaced$: Observable<BidPlacedEvent> = this.bidPlacedSubject.asObservable();
  readonly bidRejected$ = this.bidRejectedSubject.asObservable();
  readonly auctionStarted$ = this.auctionStartedSubject.asObservable();
  readonly auctionEnded$ = this.auctionEndedSubject.asObservable();
  readonly presence$ = this.presenceSubject.asObservable();
  readonly state$ = this.stateSubject.asObservable();

  constructor(private zone: NgZone) {}

  /** Conecta uma vez; chamadas seguintes reaproveitam a mesma conexão/promise. */
  ensureConnected(): Promise<void> {
    if (this.startPromise) return this.startPromise;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/auction')
      .withAutomaticReconnect()
      .build();

    connection.on('BidPlaced', (d: BidPlacedEvent) => this.emit(this.bidPlacedSubject, d));
    connection.on('BidRejected', (d) => this.emit(this.bidRejectedSubject, d));
    connection.on('AuctionStarted', (d) => this.emit(this.auctionStartedSubject, d));
    connection.on('AuctionEnded', (d: AuctionEndedEvent) => this.emit(this.auctionEndedSubject, d));
    connection.on('PresenceChanged', (d: PresenceEvent) => this.emit(this.presenceSubject, d));

    connection.onreconnecting(() => this.zone.run(() => this.stateSubject.next('reconnecting')));
    connection.onclose(() => this.zone.run(() => this.stateSubject.next('offline')));
    connection.onreconnected(() => {
      this.zone.run(() => this.stateSubject.next('live'));
      // Novo connectionId: os grupos precisam ser reassinados.
      this.watched.forEach(id => connection.invoke('JoinAuction', id).catch(() => {}));
    });

    this.connection = connection;
    this.stateSubject.next('connecting');

    this.startPromise = connection
      .start()
      .then(() => this.zone.run(() => this.stateSubject.next('live')))
      .catch(err => {
        // Permite nova tentativa na próxima vez que uma tela pedir a conexão.
        this.startPromise = null;
        this.connection = null;
        this.zone.run(() => this.stateSubject.next('offline'));
        throw err;
      });

    return this.startPromise;
  }

  async joinAuction(auctionId: number): Promise<void> {
    await this.ensureConnected();
    this.watched.add(auctionId);
    await this.connection!.invoke('JoinAuction', auctionId);
  }

  async leaveAuction(auctionId: number): Promise<void> {
    this.watched.delete(auctionId);
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveAuction', auctionId);
    }
  }

  private emit<T>(subject: Subject<T>, data: T): void {
    this.zone.run(() => subject.next(data));
  }
}
