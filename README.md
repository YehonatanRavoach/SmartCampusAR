# Smart Campus AR Navigation

**An AR-based campus navigation and management system built with Unity and Firebase.**

Watch the application demo:   
https://drive.google.com/file/d/1zOB0Gy-KgSvQ-_73IhpnsqvUIuR2az4i/view?usp=drive_link

## 🧭 Overview

Smart Campus AR is a mobile application designed to help students, visitors, and staff navigate university campuses using real-time Augmented Reality (AR). The app overlays 3D arrows and visual markers on the camera view, guiding users precisely between campus buildings.

It also includes a robust admin portal for managing campus data, buildings, and maps through Firebase's cloud services.

---

## 🎯 Key Features

### 🔹 Guest Navigation
- Live AR directional arrows and compass-based guidance.
- Building info panels with images, descriptions, and ETA.
- Map-based and list-based building selection.

### 🔹 Campus Admin Portal
- Register new campuses and manage building data.
- Upload building images, coordinates, and map placement.
- Role-based access for secure building management.

### 🔹 System Admin Dashboard
- Manage multiple campuses and admins.
- Approve or reject registration requests.
- Full control over status and deletion of entities.

---

## 🏗 Architecture

The app follows the **MVC (Model-View-Controller)** design pattern:

- **Model:** Firebase (Authentication, Firestore, Storage, Cloud Functions)
- **View:** Unity UI Toolkit + AR Foundation
- **Controller:** C# scripts managing navigation, validation, and backend communication

---

## 🔧 Tech Stack

- **Unity 2021 + AR Foundation**  
- **C# 11**  
- **Firebase (Auth, Firestore, Storage, Cloud Functions - Node.js 20)**  
- **DOTween, TMPro, UnityWebRequest**  
- **Multiplatform:** Android + iOS (via Unity build system)

Link to Android APK:   
https://drive.google.com/file/d/1uAE7R8v_V1iexEFiEyGKphfzUXIL8G3M/view?usp=sharing

---

## 📁 Project Structure

| Layer      | Description |
|------------|-------------|
| `View`     | Unity UI (e.g., Welcome Screen, AR Scene, Admin Panels) |
| `Control`  | C# scripts handling user interaction, input, and scene logic |
| `Model`    | Firebase integration for real-time backend and file storage |
| `Cloud Functions` | Handles registration, validation, approval workflows |

---

## 🚀 Getting Started

1. Clone the project and open it in Unity 2021.
2. Configure Firebase (add `google-services.json` / `GoogleService-Info.plist`).
3. Add your campus data via the admin interface or Firestore.
4. Build to Android or iOS using AR support enabled.

---

## 📷 Screenshots

> See 'Application Screen.pdf` for full screen references. [Application Screens.pdf](https://github.com/user-attachments/files/21516458/Application.Screens.pdf)


---

## 👤 Authors

Adir Shachri, Almog Bacharilia, Maxim Shapira, Oz Rotshtein, Tomer Haimov, **Yehonatan Ravoach**

Supervised by: Eran Katzav  
Date: July 2025

---

## 📄 License

This project is part of an academic submission. Contact the authors for reuse or contribution.
