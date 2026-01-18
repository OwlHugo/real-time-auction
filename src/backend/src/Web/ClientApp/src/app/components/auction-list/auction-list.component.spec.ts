import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuctionListComponent } from './auction-list.component';

describe('AuctionListComponent', () => {
  let component: AuctionListComponent;
  let fixture: ComponentFixture<AuctionListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuctionListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AuctionListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
