🐝 Edge-AI Insect Recognition & AR Visualization
Real-time Computer Vision system optimized for mobile devices using MobileNetV3 and TensorFlow Lite.

📌 Project Overview
Dự án này triển khai một hệ thống nhận diện côn trùng thời gian thực, tích hợp công nghệ Thực tế ảo (AR) nhằm hỗ trợ giáo dục trực quan cho trẻ em. Thách thức lớn nhất của dự án là duy trì độ chính xác cao trong khi vẫn đảm bảo tốc độ suy luận (Inference speed) mượt mà trên các thiết bị di động có cấu hình phần cứng hạn chế.

🧠 AI Engineering & Optimization (Key Highlights)
Để đưa được mô hình Deep Learning từ môi trường nghiên cứu (Python) lên ứng dụng thực tế (Mobile), tôi đã thực hiện các giải pháp kỹ thuật sau:

1. Model Architecture & Training
Backbone: Sử dụng MobileNetV3-Small – được thiết kế với kiến trúc Neural Architecture Search (NAS), tối ưu riêng cho CPU điện thoại.

Transfer Learning: Tận dụng pre-trained weights từ ImageNet để trích xuất đặc trưng mạnh mẽ dù tập dữ liệu đầu vào giới hạn.

Preprocessing Pipeline: Áp dụng Data Augmentation (Random Rotation, Zoom, Horizontal Flip) để tăng khả năng tổng quát hóa, giúp model nhận diện tốt ngay cả khi trẻ em cầm điện thoại ở các góc độ không chuẩn.

2. On-Device Optimization (TFLite)
Post-Training Quantization (INT8): Chuyển đổi trọng số từ Float32 sang Integer 8-bit.

Kết quả: Giảm dung lượng model xuống ~4MB (tiết kiệm 75% bộ nhớ).

Hiệu năng: Tăng tốc độ xử lý trên CPU lên gấp 3 lần, giảm hiện tượng nóng máy khi sử dụng lâu.

Inference Latency: Đạt mức ~30ms/frame, đảm bảo trải nghiệm real-time không độ trễ.

3. Image Processing Workflow
Hệ thống tự động thực hiện Image Resizing (224x224) và Normalization trước khi đưa vào tensor đầu vào.

Cơ chế Probability Thresholding: Chỉ hiển thị kết quả khi độ tin cậy (Confidence Score) > 0.7 để tránh tình trạng nhận diện sai (False Positive).

🚀 Key Features
Instant Identification: Nhận diện 11 loài côn trùng phổ biến (Ong, Bướm, Kiến, Chuồn chuồn, Gián, Bọ hung, Muỗi, Nhện, Bọ chân dài, Bọ rùa, Sâu bướm).

3D Interactive AR: Tự động kích hoạt mô hình 3D tương ứng sau khi nhận diện thành công, cho phép trẻ xoay/phóng to để quan sát.

Offline Processing: Toàn bộ quá trình xử lý AI diễn ra 100% trên thiết bị, không cần kết nối Internet, đảm bảo quyền riêng tư và tốc độ.

🛠 Tech Stack
AI/ML: Python, TensorFlow, Keras, OpenCV.

Mobile App: Unity Engine, Chttps://www.google.com/search?q=%23 (Logic handling & UI).

Deployment: TensorFlow Lite SDK for Unity.

📊 Performance Metrics

| Metric | Value |
| :--- | :--- |
| **Accuracy (Top-1)** | 92.4% |
| **Inference Time (Mobile CPU)** | 25ms - 35ms |
| **Model Size** | 3.8 MB |
| **Target OS** | Android 7.0 (API 24) or higher |

🎥 Demo & Screenshots

Unity hiển thị nội dung giáo dục và mô hình 3D
<img width="966" height="570" alt="image" src="https://github.com/user-attachments/assets/c3417539-ad74-4be3-9a88-cd4221204923" />

Link YouToBe: Để xem chi tiết quá trình hoạt động của ứng dụng (Nhận diện, Hiển thị mô hình 3D và Mini-game), vui lòng xem video dưới đây:

[![Watch the video](https://img.youtube.com/vi/W12V03jDgb0/0.jpg)](https://youtube.com/shorts/W12V03jDgb0)


