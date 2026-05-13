# 🐝 Edge-AI Insect Recognition & AR Visualization

Real-time Computer Vision system optimized for mobile devices using MobileNetV3 and TensorFlow Lite.

---

# 📌 Project Overview

This project implements a real-time insect recognition system integrated with Augmented Reality (AR) technology to support interactive educational experiences for children. The primary challenge of the project was maintaining high recognition accuracy while ensuring smooth inference speed on resource-constrained mobile devices.

---

# 🧠 AI Engineering & Optimization (Key Highlights)

To deploy the Deep Learning model from a research environment (Python) into a real-world mobile application, several optimization techniques were applied.

## 1. Model Architecture & Training

### Backbone
Used **MobileNetV3-Small**, a lightweight architecture designed through Neural Architecture Search (NAS) and optimized specifically for mobile CPUs.

### Transfer Learning
Leveraged pre-trained ImageNet weights to extract robust visual features even with a relatively limited dataset.

### Preprocessing Pipeline
Applied extensive Data Augmentation techniques including:
- Random Rotation
- Zoom
- Horizontal Flip

These augmentations improved model generalization and enabled robust recognition under non-ideal camera angles commonly produced by children during usage.

---

## 2. On-Device Optimization (TensorFlow Lite)

### Post-Training Quantization (INT8)
Converted model weights from Float32 to 8-bit integers.

### Results
- Reduced model size to approximately **4MB** (around 75% memory reduction)
- Achieved approximately **3× faster CPU inference**
- Reduced thermal issues during prolonged mobile usage

### Inference Latency
Achieved approximately **30ms/frame**, ensuring smooth real-time performance with minimal latency.

---

## 3. Image Processing Workflow

The system automatically performs:
- Image Resizing (224×224)
- Input Normalization

before feeding data into the inference tensor pipeline.

### Probability Thresholding
Implemented a confidence threshold mechanism:
- Predictions are displayed only when the confidence score exceeds **0.7**
- Helps reduce false positives and unstable predictions

---

# 🚀 Key Features

## Instant Identification
Recognizes 11 common insect species including:
- Bee
- Butterfly
- Ant
- Dragonfly
- Cockroach
- Beetle
- Mosquito
- Spider
- Harvestman
- Ladybug
- Caterpillar

## 3D Interactive AR
Automatically activates corresponding 3D AR models after successful recognition, allowing children to:
- Rotate models
- Zoom in/out
- Interactively explore insect structures

## Offline Processing
All AI inference is performed entirely on-device:
- No Internet connection required
- Faster response time
- Better privacy protection

---

# 🛠 Tech Stack

## AI/ML
- Python
- TensorFlow
- Keras
- OpenCV

## Mobile Application
- Unity Engine

## Deployment
- TensorFlow Lite SDK for Unity

---

# 📊 Performance Metrics

| Metric | Value |
| :--- | :--- |
| **Accuracy (Top-1)** | 92.4% |
| **Inference Time (Mobile CPU)** | 25ms - 35ms |
| **Model Size** | 3.8 MB |
| **Target OS** | Android 7.0 (API 24) or higher |

---

# 🎥 Demo & Screenshots

## YouTube Demo

Watch the full demonstration of:
- Real-time insect recognition
- AR visualization
- Interactive 3D models
- Mini-game integration

[![Watch the video](https://img.youtube.com/vi/W12V03jDgb0/0.jpg)](https://youtube.com/shorts/W12V03jDgb0)
