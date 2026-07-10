# Danh Sách Chức Năng Hệ Thống: Dự Án RazorPage Gốc (`CourseSphere`)

Tài liệu này tổng hợp toàn bộ các tính năng, phân quyền người dùng và kiến trúc công nghệ của dự án gốc viết bằng ASP.NET Core Razor Pages (`CourseSphere`) nhằm giúp các thành viên mới gia nhập đội ngũ phát triển dễ dàng nắm bắt bức tranh toàn cảnh của hệ thống để tiến hành xây dựng lại phiên bản Next.js Frontend và C# Web API Backend.

---

## 🛠️ Tổng Quan Công Nghệ (Technical Stack)
* **Web Frontend / Backend**: C# ASP.NET Core Razor Pages
* **ORM & Database**: Entity Framework Core + SQL Server
* **Thanh Toán (Payment)**: Tích hợp cổng thanh toán **PayOS** (Tạo link thanh toán VietQR chuyển khoản ngân hàng và tự động đồng bộ trạng thái qua Webhook).
* **Trí tuệ nhân tạo (AI Services)**: Sử dụng AWS AI Services (giao tiếp qua S3, Lambda, API Gateway):
  - **AWS Transcribe / Bedrock / Claude**: Chấm điểm thi nói (Speaking) và viết (Writing) chuẩn IELTS.
  - **AI Generator**: Tự động sinh bộ câu hỏi trắc nghiệm từ tệp tài liệu PDF tải lên.
* **Thời gian thực (Real-time)**: **SignalR** (RealtimeHub) dùng cho tính năng trò chuyện trực tiếp (Chat) và thông báo hệ thống.

---

## 👥 Vai Trò Người Dùng & Phân Quyền (User Roles)

Hệ thống được chia làm **3 vai trò chính** với các luồng nghiệp vụ khép kín:
1. **Học viên (Student)**: Tìm kiếm, đăng ký học, thanh toán khóa học, xem bài giảng (video/bài đọc), làm trắc nghiệm, luyện thi Speaking/Writing bằng AI, nhắn tin thảo luận và nhận chứng chỉ tốt nghiệp.
2. **Giảng viên (Instructor / Teacher)**: Tạo khóa học, soạn thảo chương trình học (Curriculum Builder), thiết lập bài tập trắc nghiệm tự động hoặc bằng AI, nhắn tin giải đáp học viên và quản lý ví doanh thu/rút tiền.
3. **Quản trị viên (Admin)**: Xem thống kê hệ thống, quản lý tài khoản thành viên (phân quyền/khóa tài khoản), kiểm duyệt nội dung khóa học trước khi xuất bản, và phê duyệt các lệnh rút tiền của giảng viên.

---

## 📋 Chi Tiết Danh Sách Chức Năng Theo Phân Hệ

### 🔐 1. Phân Hệ Xác Thực & Tài Khoản (Authentication & Profile)
* **Đăng ký tài khoản (Register)**: Học viên/Giảng viên đăng ký bằng Email và Mật khẩu. Hệ thống gửi mã/link xác thực về Email để kích hoạt tài khoản (`Verify`).
* **Đăng nhập / Đăng xuất (Login/Logout)**: Đăng nhập phân quyền bảo mật, tự động chuyển hướng người dùng về đúng khu vực tương ứng (Admin Dashboard, Instructor Dashboard hoặc Home Page).
* **Quản lý Hồ sơ (Profile Management)**: Xem và cập nhật thông tin cá nhân bao gồm Họ tên, Số điện thoại, ảnh đại diện (Avatar), chức danh khoa học và tiểu sử (Bio).

---

### 📖 2. Phân Hệ Dành Cho Học Viên (Student Features)

#### A. Khám Phá & Đăng Ký Khóa Học
* **Trang chủ & Catalog Khóa học**:
  - Ô tìm kiếm khóa học theo từ khóa ở đầu trang.
  - Bộ lọc khóa học theo Ngôn ngữ giảng dạy (Tiếng Việt, Tiếng Anh) và Mức giá (Miễn phí, Có phí).
* **Trang Chi Tiết Khóa Học (Course Detail)**:
  - Xem thông tin tổng quan về khóa học: Tiêu đề, mô tả chi tiết, ảnh bìa, ngôn ngữ, giảng viên hướng dẫn.
  - Xem danh sách chương mục và bài học của khóa học (Đề cương).
  - Đăng ký học: Đối với khóa học miễn phí, học viên tham gia lớp ngay lập tức. Đối với khóa học trả phí, chuyển tiếp sang giao diện thanh toán.
* **Thanh Toán (Payment Integration)**:
  - Tạo liên kết thanh toán an toàn thông qua cổng **PayOS**.
  - Hiển thị mã QR ngân hàng (VietQR) kèm số tiền và nội dung chuyển khoản tự động.
  - Trạng thái thanh toán cập nhật tự động (Real-time) sau khi người dùng quét mã chuyển tiền thành công.

#### B. Không Gian Học Tập Trực Tuyến (Learning Space)
* **Trình xem Học liệu đa phương tiện**:
  - **Video Lesson**: Xem video hướng dẫn chất lượng cao (Youtube Embedded hoặc video MP4 tải lên từ AWS S3).
  - **Reading Article**: Đọc các bài viết, tài liệu lý thuyết biên soạn trực tiếp.
  - **Lesson Resources**: Cho phép tải xuống các tài liệu đính kèm bài học (File PDF, Slide, Source code mẫu).
* **Đánh dấu hoàn thành (Mark as Complete)**: Đánh dấu bài học đã xem xong để cập nhật tiến trình học tập cá nhân (`Progress Percent%`).

#### C. Luyện Tập & Đánh Giá Năng Lực (AI-Powered Practice)
* **Làm bài trắc nghiệm (Quiz Attempt)**:
  - Làm bài kiểm tra trắc nghiệm nhiều lựa chọn trực tiếp trên giao diện.
  - Hệ thống tự động chấm điểm ngay sau khi nộp bài, ghi nhận kết quả Đạt (Pass) hay Chưa đạt (Fail) dựa trên điểm số yêu cầu (`Passing Score`).
  - Xem lại lịch sử thi và số lần làm bài còn lại.
* **Luyện thi viết IELTS thông minh (AI IELTS Writing)**:
  - Học viên viết bài luận tiếng Anh dựa trên đề bài (Prompt) được cung cấp.
  - Hệ thống gọi API AI đánh giá bài viết dựa trên 4 tiêu chí IELTS và trả về điểm số tổng quan (`Band Score`) kèm nhận xét sửa lỗi ngữ pháp, từ vựng chi tiết.
* **Luyện thi nói IELTS thông minh (AI IELTS Speaking)**:
  - Học viên ghi âm câu trả lời nói trực tiếp trên trình duyệt.
  - File ghi âm được đẩy lên AWS S3, sau đó chạy phân tích âm thanh bằng AI để phản hồi chi tiết về độ phát âm chính xác, độ trôi chảy, ngữ pháp và chấm điểm band score tương ứng.

#### D. Tương Tác & Chứng Nhận
* **Nhận Chứng chỉ Điện tử (Certificates)**:
  - Tự động mở khóa chứng chỉ hoàn thành khóa học xuất sắc khi tiến độ đạt 100%.
  - Hiển thị chứng chỉ với thiết kế trang trọng gồm: Tên học viên, tên khóa học, giảng viên hướng dẫn, ngày cấp và mã xác thực bảo mật duy nhất (`CertificateId`).
  - Hỗ trợ In hoặc Lưu trực tiếp thành file PDF.
* **Nhắn tin cho Giảng viên (Direct Chat)**: Kênh chat SignalR giúp học viên thảo luận, gửi câu hỏi trực tiếp cho giảng viên hướng dẫn khóa học đó.

---

### 🎓 3. Phân Hệ Dành Cho Giảng Viên (Instructor Features)

#### A. Tổng Quan Doanh Thu & Thống Kê
* **Bảng điều khiển Giảng viên (Instructor Dashboard)**:
  - Các thẻ chỉ số: Tổng doanh thu đã kiếm được, số lượng học viên đăng ký, số lượng khóa học đã xuất bản, đánh giá trung bình.
  - Biểu đồ khu vực (Area Chart) thống kê doanh thu theo từng tháng trong nửa năm gần nhất.
  - Danh sách các khóa học phổ biến, thu hút nhiều lượt đăng ký nhất.

#### B. Quản Lý Khóa Học & Đề Cương (Curriculum Builder)
* **Tạo và Chỉnh sửa Khóa học**: Thiết lập thông tin nền gồm tên khóa học, giá bán (VND), ngôn ngữ giảng dạy, ảnh đại diện.
* **Trình dựng Đề cương (Curriculum Builder)**:
  - **Quản lý Chương (Module Section)**: Thêm mới chương, sửa tên, mô tả chương, hoặc xóa chương học.
  - **Quản lý Bài học (Lesson)**: Thêm bài học con trong từng chương mục.
  - **Thêm Học liệu (Material Add)**: Mở Slide Sheet panel từ bên phải để thêm các loại bài học:
    - *Video*: Upload tệp MP4 lên S3 hoặc nhập link YouTube.
    - *Reading*: Viết nội dung tài liệu lý thuyết trực tiếp.
    - *Quiz*: Xây dựng bài kiểm tra trắc nghiệm bằng cách thêm các câu hỏi, nhập lựa chọn và tích chọn đáp án đúng.
* **AI Quiz Generator**: Giảng viên tải lên 1 tệp tài liệu PDF (giáo trình/slide), hệ thống tự động trích xuất nội dung và sinh ra bộ câu hỏi trắc nghiệm tương ứng, giảng viên chỉ cần chỉnh sửa lại nếu cần và lưu vào giáo án.

#### C. Quản Lý Ví & Tài Chính (Wallet & Payout)
* **Ví giảng viên**: Ghi nhận số dư tiền mặt khả dụng (nhận 90% doanh thu mỗi khi học viên mua khóa học thành công).
* **Yêu cầu rút tiền (Withdrawal Request)**:
  - Giảng viên điền form yêu cầu rút tiền về tài khoản ngân hàng (Chọn ngân hàng, Số tài khoản, Tên chủ tài khoản, Số tiền rút).
  - Hệ thống kiểm tra điều kiện rút (Số tiền tối thiểu 50.000đ và nhỏ hơn số dư ví hiện tại).
* **Lịch sử giao dịch**: Danh sách tất cả các biến động số dư (Cộng tiền doanh thu học viên đăng ký (+), Trừ tiền khi gửi yêu cầu rút (-)) kèm trạng thái (Đang duyệt, Thành công, Thất bại).

---

### 👑 4. Phân Hệ Dành Cho Quản Trị Viên (Admin Features)

#### A. Tổng Quan Hệ Thống & Thống Kê
* **Bảng điều khiển Admin (Admin Dashboard)**:
  - Xem tổng doanh thu toàn bộ nền tảng OLP Academy.
  - Xem biểu đồ cột doanh thu hệ thống theo các tháng.
  - Biểu đồ tròn cơ cấu tài khoản người dùng (Admin, Giảng viên, Học viên).
  - Đếm số lượng khóa học đang chờ kiểm duyệt.

#### B. Quản Trị Người Dùng (User Management)
* **Tra cứu & Bộ lọc thành viên**: Tìm kiếm tài khoản theo tên/email, lọc theo phân quyền hoặc trạng thái hoạt động.
* **Phân quyền người dùng**: Cho phép chuyển đổi vai trò của một tài khoản (Ví dụ: Nâng cấp tài khoản học viên lên giảng viên).
* **Khóa / Mở khóa tài khoản (Lock/Unlock)**: Khóa các tài khoản vi phạm chính sách hoặc mở khóa truy cập cho thành viên.

#### C. Phê Duyệt Khóa Học & Lệnh Rút Tiền
* **Kiểm duyệt Khóa học (Course Review)**:
  - Xem danh sách các khóa học do giảng viên nộp duyệt để xin xuất bản công khai.
  - Xem chi tiết đề cương khóa học để đánh giá chất lượng.
  - **Phê duyệt (Approve)**: Chuyển trạng thái khóa học sang *Published* để hiển thị công khai trên catalog.
  - **Từ chối (Reject)**: Nhập lý do từ chối (Ví dụ: Thiếu tài liệu bài tập, video mờ...) thông qua hộp thoại modal để giảng viên nhận phản hồi và sửa đổi.
* **Phê duyệt Rút tiền (Payout Review)**:
  - Xem danh sách yêu cầu thanh toán/rút tiền của các giảng viên.
  - Sau khi Admin thực hiện chuyển khoản ngân hàng ngoài đời thực, click **Phê duyệt** để hệ thống xác nhận giao dịch thành công và cập nhật trạng thái lịch sử ví của giảng viên.
