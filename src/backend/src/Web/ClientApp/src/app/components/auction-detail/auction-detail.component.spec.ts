import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuctionDetailComponent } from './auction-detail.component';

describe('AuctionDetailComponent', () => {
  let component: AuctionDetailComponent;
  let fixture: ComponentFixture<AuctionDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuctionDetailComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AuctionDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
