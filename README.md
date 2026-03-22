# DynoTune

DynoTune is a Windows desktop application for workload-aware power, thermal, and noise optimization on AMD-based PC systems. It monitors real-time system telemetry, classifies active workloads, and applies safe tuning profiles to improve efficiency, reduce heat, and lower noise while maintaining stable performance.

This project is being developed as a diploma/thesis project.

## Overview

Modern PC systems often run with static, one-size-fits-all settings that do not adapt to actual user activity. DynoTune aims to solve this by adjusting system behavior based on workload type such as idle use, browsing, office tasks, gaming, or sustained heavy workloads.

The application combines monitoring, logging, workload classification, and tuning control into a single Windows app with a simple interface.

## Goals

- Monitor CPU, GPU, memory, temperature, power, clock, and fan data
- Log system telemetry for later analysis
- Detect and classify the current workload
- Apply safe tuning profiles depending on workload state
- Improve energy efficiency and thermal behavior
- Reduce unnecessary fan noise
- Provide experiment data for thesis evaluation

## Main Features

### Monitoring
- Real-time hardware telemetry monitoring
- CPU metrics using LibreHardwareMonitor
- GPU metrics and controls using AMD ADLX
- Live tracking of usage, clocks, temperatures, power, and fan speed

### Logging
- Continuous telemetry logging
- CSV export for analysis and thesis graphs
- Session-based experiment records
- Profile and workload state logging

### Workload Classification
- Rule-based workload detection
- Initial workload categories:
  - Idle
  - Browsing
  - Office
  - Media
  - Gaming
  - Rendering
  - Unknown

### Tuning
- Safe profile-based tuning
- AMD GPU tuning integration through ADLX
- CPU tuning through Windows built-in power management
- Planned fan and thermal policy controls

### User Interface
- Built with WinUI 3
- Dashboard for current system state
- Monitoring page
- Profiles page
- Tuning page
- Logs page
- Settings page

## Technology Stack

- **Language:** C#
- **UI Framework:** WinUI 3
- **IDE:** Visual Studio
- **Hardware Monitoring:** LibreHardwareMonitor
- **GPU Control / Telemetry:** AMD ADLX
- **Platform:** Windows
- **Architecture:** MVVM-inspired modular structure

## Project Structure

```text
DynoTune
 ├─ Assets
 ├─ Models
 ├─ Services
 ├─ ViewModels
 ├─ Views
 ├─ App.xaml
 └─ MainWindow.xaml
