import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/auth';
  private isLoggedInSubject = new BehaviorSubject<boolean>(false);
  private currentUserSubject = new BehaviorSubject<any>(null);
  public isLoggedIn$ = this.isLoggedInSubject.asObservable();
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.checkAuth().subscribe();
  }

  checkAuth(): Observable<any> {
    return this.http.get(`${this.apiUrl}/me`, { withCredentials: true }).pipe(
      tap((user) => {
        this.isLoggedInSubject.next(true);
        this.currentUserSubject.next(user);
      }),
      catchError(err => {
        this.isLoggedInSubject.next(false);
        this.currentUserSubject.next(null);
        return throwError(() => err);
      })
    );
  }

  register(email: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, { email, password });
  }

  login(email: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/login?useCookies=true`, { email, password }, { withCredentials: true })
      .pipe(
        switchMap(() => this.checkAuth())
      );
  }

  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true }).pipe(
      catchError(() => new Observable(o => o.next(null))),
      tap(() => this.isLoggedInSubject.next(false))
    );
  }
}
