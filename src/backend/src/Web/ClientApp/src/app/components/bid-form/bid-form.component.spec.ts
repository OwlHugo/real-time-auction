import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BidFormComponent } from './bid-form.component';

describe('BidFormComponent', () => {
  let component: BidFormComponent;
  let fixture: ComponentFixture<BidFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BidFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BidFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
