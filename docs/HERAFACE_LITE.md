# HeraFace Lite SDK

## Mục tiêu

HeraFace Lite là phiên bản SDK thu gọn dành cho máy tính nhúng cá nhân và thiết bị edge nhẹ, cho phép:

- phát hiện khuôn mặt trong video trực tiếp
- tạo embedding cho từng khuôn mặt
- lưu tối đa khoảng 50 hồ sơ người dùng
- so khớp local face database bằng cosine similarity
- hiển thị tên người trên OSD hoặc xuất event đơn giản

## Kiến trúc đề xuất

```text
Camera / RTSP / File
        ↓
Face Detector (YuNet)
        ↓
Feature Encoder (SFace)
        ↓
Local Face DB (<= 50 identities)
        ↓
OSD / Broker / Output
```

## Tại sao phù hợp cho 50 khuôn mặt?

Với số lượng người dùng nhỏ, không cần vector database phức tạp như Milvus ngay từ đầu. Cách đơn giản, dễ triển khai hơn là:

- lưu embedding local trong SQLite, JSON hoặc CSV
- so sánh embedding mới với danh sách hiện có
- dựa trên bộ ngưỡng similarity hợp lý (ví dụ 0.85)

Điều này rất phù hợp cho:

- máy cửa tự động
- máy chấm công nhỏ
- thiết bị kiểm soát truy cập cá nhân
- demo/PoC trên board nhúng

## Cấu trúc dữ liệu

Mỗi hồ sơ nên có:

- user_id
- name
- embedding
- created_at
- optional photo_path

## Đề xuất workflow

### 1. Enroll user

- chụp ảnh khuôn mặt rõ nét
- trích xuất embedding
- lưu vào local database

### 2. Recognition

- phát hiện khuôn mặt trong frame
- trích xuất embedding frame hiện tại
- tính khoảng cách so với các embedding đã lưu
- nếu tốt nhất > ngưỡng, xác định user

### 3. Output

- hiển thị tên lên khung hình
- gửi event qua MQTT/Webhook nếu cần
- lưu log check-in/check-out

## Ví dụ mã phát triển nhanh

Xem mẫu tại [../samples/04_hera_face_lite/main.cpp](../samples/04_hera_face_lite/main.cpp)

Mẫu này minh họa:

- source video đầu vào
- YuNet face detector
- SFace feature encoder
- local face database
- OSD và web output

## Khuyến nghị triển khai

### CPU-first

Với 50 người, ưu tiên:

- YuNet nhẹ cho detection
- SFace hoặc model embedding nhẹ cho recognition
- local DB thay vì vector DB ở giai đoạn đầu

### Kích thước phần cứng

- RAM: >= 4 GB
- CPU: 4 lõi trở lên
- GPU: không bắt buộc
- camera: 720p/1080p

## Khi nào nên nâng cấp lên vector DB?

Khi số lượng người dùng vượt quá ~200–500, hoặc cần:

- tìm kiếm nhiều người cùng lúc
- quản lý nhiều camera
- tăng tốc xử lý nhiều stream

Lúc đó nên dùng Milvus hoặc Qdrant thay cho local DB.

## Kết luận

HeraFace Lite là phiên bản phù hợp để triển khai nhanh trên thiết bị cá nhân và board nhúng, mà vẫn cho phép nhận diện được khoảng 50 khuôn mặt với cấu trúc đơn giản, dễ tích hợp và dễ triển khai cho người dùng phát triển.