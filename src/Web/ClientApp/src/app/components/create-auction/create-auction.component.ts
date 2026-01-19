import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuctionService } from '../../services/auction.service';

@Component({
  selector: 'app-create-auction',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './create-auction.component.html',
})
export class CreateAuctionComponent {
  auctionForm: FormGroup;
  errorMessage: string | null = null;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private auctionService: AuctionService,
    private router: Router
  ) {
    this.auctionForm = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required],
      startingPrice: [100, [Validators.required, Validators.min(1)]],
      reservePrice: [null],
      startTime: ['', Validators.required],
      endTime: ['', Validators.required]
    });
  }

  onSubmit(): void {
    if (this.auctionForm.valid) {
      this.isSubmitting = true;
      this.errorMessage = null;

      const formValue = this.auctionForm.value;

      const auctionData = {
        ...formValue,
        startTime: new Date(formValue.startTime),
        endTime: new Date(formValue.endTime)
      };

      this.auctionService.createAuction(auctionData).subscribe({
        next: (res) => {
          this.isSubmitting = false;
          const id = res?.id || res;
          if (typeof id === 'number') {
            this.router.navigate(['/auctions', id]);
          } else if (id && id.id) {
            this.router.navigate(['/auctions', id.id]);
          } else {
            this.router.navigate(['/auctions']);
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          console.error('Failed to create auction', err);
          this.errorMessage = err.error?.detail || err.error?.title || 'Ocorreu um erro ao publicar o leilão. Verifique os dados.';
        }
      });
    }
  }
}
