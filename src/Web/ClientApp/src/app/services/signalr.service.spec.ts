import { TestBed } from '@angular/core/testing';
import { SignalRService } from './signalr.service';

describe('SignalRService', () => {
  let service: SignalRService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SignalRService);
  });

  it('should be created offline, before any screen connects', () => {
    expect(service).toBeTruthy();
    let state: string | undefined;
    service.state$.subscribe(s => (state = s)).unsubscribe();
    expect(state).toBe('offline');
  });

  it('exposes one stream per server event', () => {
    expect(service.bidPlaced$).toBeDefined();
    expect(service.bidRejected$).toBeDefined();
    expect(service.auctionStarted$).toBeDefined();
    expect(service.auctionEnded$).toBeDefined();
    expect(service.presence$).toBeDefined();
  });
});
