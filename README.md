# Arrematch - Real-Time Auction Platform 🚀

A high-performance, distributed auction platform built to demonstrate advanced software engineering concepts using **.NET 10** and **Angular**.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-19-red)](https://angular.io/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Messaging-orange)](https://www.rabbitmq.com/)

## 🏗️ Technical Stack

This project leverages a modern, distributed stack designed for resilience and scale:

*   **Backend:** .NET 10 (ASP.NET Core Web API)
*   **Frontend:** Angular (SPA) with TailwindCSS
*   **Real-time Communication:** SignalR with Redis Backplane (optimistic scaling)
*   **Messaging & Event-Driven Architecture:** MassTransit with RabbitMQ
*   **Database:** PostgreSQL with EF Core
*   **Observability:** Prometheus & Grafana metrics
*   **Architecture:** Clean Architecture + CQRS (Command Query Responsibility Segregation)

## ⚡ Key Engineering Challenges Solved

This isn't just a CRUD application. It tackles complex distributed system problems:

### 1. Concurrency & Race Conditions 🏁
In an auction, milliseconds matter. To prevent two users from placing the same bid simultaneously:
*   Implemented **Optimistic Concurrency Control** using `RowVersion` within PostgreSQL.
*   The database rejects conflicting updates at the transaction level, ensuring data integrity without locking the entire table.

### 2. Idempotency 🔄
Network failures can cause requests to be retried.
*   Custom **Idempotency Filters** prevent the execution of the same bid or payment operation multiple times.
*   Uses a unique `X-Idempotency-Key` header tracked in a distributed cache.

### 3. Eventual Consistency & Resilience 📉
Instead of processing everything in the request thread (blocking the user):
*   Heavy operations (like auction closure and email notifications) are offloaded to background workers via **RabbitMQ**.
*   The system remains responsive even under high load.

### 4. Real-Time Updates 📡
*   **SignalR** pushes live bid updates to all connected clients instantly.
*   No "refresh page" needed. The UI updates the price and notifies if you've been outbid in real-time.

## 🚀 How to Run Locally

### Prerequisites
*   Docker & Docker Compose
*   .NET 10 SDK
*   Node.js & NPM

### Steps

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/YOUR_USERNAME/real-time-auction.git
    cd real-time-auction
    ```

2.  **Start Infrastructure (Postgres, RabbitMQ, Redis):**
    ```bash
    docker-compose up -d
    ```

3.  **Run the Backend:**
    ```bash
    dotnet run --project src/Web/Web.csproj
    ```

4.  **Run the Frontend:**
    The frontend is automatically launched by the ASP.NET Core backend.

    *   Wait for the line: `Angular Live Development Server is listening on localhost:44447`
    *   Access the app at: `https://localhost:44447`

    *(Optional) To run independently:*
    ```bash
    cd src/Web/ClientApp
    npm install
    npm start
    ```

5.  **Access:**
    Open `https://localhost:5001` (or the port indicated in your console).

## 📊 Observability

The application exposes metrics at `/metrics` for Prometheus.
*   **`app_bids_total`**: Counter for total bids received.
*   **`app_bid_amount`**: Histogram of bid values.

## 🤝 Contribution

Feel free to open issues or PRs if you want to discuss distributed architecture or .NET optimizations!

## 📜 License

This project is licensed under the MIT License.
