# RV Park Management System

A full-stack web application built for managing RV park reservations, customers, sites, employees and payments.

This was a group project for **CS 3750** at Weber State University. The goal was to build a working reservation system that supports both customers and park staff while handling the different tasks involved in running an RV park.

> This repository is a fork of the original team project. I contributed to the project as part of a six-person development team.

## What the App Does

Customers can search for available RV sites, create reservations and manage their stays.

Employees and administrators have additional tools for managing reservations, customers, sites, fees and other day-to-day park operations.

Some of the main features include:

* Customer registration and login
* RV site search and availability
* Reservation creation and management
* Walk-in reservations
* Customer lookup during reservations
* Check-in and check-out
* Reservation cancellation
* Employee reservation management
* Admin dashboard and management tools
* Site and site type management
* Maintenance tracking
* Fees and reservation cost calculations
* Payment processing
* Stripe integration
* Cash, check and manual card payment options
* Role-based access for customers, employees and administrators
* Reports for reservation and park activity

## My Contributions

My main work on the project focused on the reservation management side of the application.

I worked on areas including:

* Reservation management for staff
* Searching and filtering reservations
* Editing and cancelling reservations
* Walk-in reservation flow
* Customer lookup and creation during walk-in reservations
* Admin reservation tools
* Employee reservation tools
* Site availability and reservation-related pages
* Check-in and check-out functionality
* Dashboard and reservation UI improvements
* Testing and debugging reservation features
* Database and migration fixes during development
* Integrating my work with features developed by other team members

Working on this project also gave me experience dealing with a larger shared codebase, Git branches, pull requests, merge conflicts and debugging features that depended on code written by multiple team members.

## Tech Stack

* **C#**
* **ASP.NET Core MVC**
* **.NET 8**
* **Entity Framework Core**
* **SQL Server**
* **Razor Views**
* **HTML / CSS**
* **JavaScript**
* **Stripe API**
* **Git & GitHub**

## Project Structure

```text
RVPark/
├── RVSite/
│   ├── Controllers/
│   ├── Data/
│   ├── Migrations/
│   ├── Models/
│   ├── Services/
│   ├── Views/
│   ├── wwwroot/
│   └── Program.cs
│
├── RVSite.slnx
└── README.md
```

The project follows the MVC pattern:

* **Models** handle application and database data
* **Views** contain the user interface
* **Controllers** handle requests and application logic
* **Services** contain shared business logic such as payments, reservation policies and cost calculations

## Running the Project

### Requirements

Make sure you have:

* .NET 8 SDK
* SQL Server
* Git

### Clone the repository

```bash
git clone https://github.com/samanchapagain/RVPark.git
cd RVPark
```

### Restore packages

```bash
dotnet restore
```

### Configure the database

Update the appropriate connection string in the application's configuration for your SQL Server database.

The application uses Entity Framework Core migrations to create and update the database.

### Run the application

```bash
cd RVSite
dotnet run
```

Open the local URL shown in the terminal.

## Payment Processing

The application includes Stripe integration along with support for other payment methods used by staff.

Stripe should be configured with your own test credentials when running the project locally. API keys and other secrets should not be committed to the repository.

## Team

This project was built collaboratively by:

* Xander Rodriguez
* Saman Chapagain
* Megan Burton
* Casey Jones
* Lindsey West
* Dallin Dutson

## What I Learned

This project gave me experience working on a web application that was much larger than an individual class project.

I got more comfortable with ASP.NET Core MVC, Entity Framework, SQL Server and working with an existing database structure. I also learned a lot about Git collaboration because different parts of the application were being developed at the same time.

One of the biggest parts of the experience was integrating features together and debugging problems that only appeared once multiple parts of the application were connected.

## About This Repository

This is my personal fork of the original team repository so I can keep the project on my GitHub profile and show the work I contributed to.

The full application was a team effort, and the sections above describe the areas I personally spent most of my time working on.
