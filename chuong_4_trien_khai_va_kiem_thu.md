# CHƯƠNG 4. TRIỂN KHAI VÀ KIỂM THỬ

## 4.1. Môi trường phát triển

### 4.1.1. Môi trường phần mềm và công cụ phát triển
Để đảm bảo tính nhất quán, hiệu năng đồ họa và khả năng chạy đa nền tảng tốt nhất cho hệ thống mô phỏng, dự án đã lựa chọn và sử dụng các công cụ phát triển phần mềm sau:
*   **Game Engine chính:** **Unity 6 (Phiên bản LTS 6000.4.7f1)**. Việc sử dụng phiên bản LTS mới nhất mang lại nhiều ưu thế vượt trội về hiệu năng kết xuất WebGL, hệ thống URP (Universal Render Pipeline) cải tiến, và bộ mô phỏng vật lý bánh xe nâng cao hoạt động chính xác hơn các phiên bản cũ.
*   **IDE (Môi trường phát triển tích hợp):** **Microsoft Visual Studio 2022** kết hợp **JetBrains Rider**. Hỗ trợ đắc lực trong việc lập trình ngôn ngữ C#, gỡ lỗi trực quan (Visual Debugging), kiểm tra nhanh cú pháp và tự động hóa biên dịch trong Unity Editor.
*   **Hệ quản trị dữ liệu:** Định dạng file tĩnh **JSON** (`theory_questions.json`) được sử dụng để lưu trữ bộ đề thi lý thuyết và lớp bộ nhớ đệm `PlayerPrefs` dùng để lưu trữ cài đặt người dùng trên hệ thống máy tính.

### 4.1.2. Môi trường phần cứng và thiết bị thử nghiệm
*   **Cấu hình máy phát triển (Development PC - Cấu hình thực tế):**
    *   Bộ vi xử lý: Intel Core i5-12400F (12th Gen)
    *   Bộ nhớ trong (RAM): 32 GB
    *   Card đồ họa (GPU): AMD Radeon RX 6600 (8GB VRAM)
    *   Hệ điều hành: Microsoft Windows 11 Pro 64-bit
*   **Thiết bị chạy thử nghiệm của người dùng cuối (Target Devices):**
    *   Máy tính để bàn hoặc Laptop cá nhân chạy hệ điều hành Windows 10/11 (sử dụng thông qua tệp bộ cài đặt đóng gói `.exe`).
    *   Các thiết bị truy cập thông qua trình duyệt Web hiện đại (Google Chrome, Microsoft Edge, Mozilla Firefox) hỗ trợ chuẩn HTML5 và đồ họa WebGL 2.0.

---

## 4.2. Hiện thực các chức năng

### 4.2.1. Module Điều khiển xe (`CarController.cs`)
Hệ thống vật lý xe hơi được hiện thực hóa dựa trên việc tương tác lực thời gian thực giữa các bánh xe và bề mặt sa hình:
*   **Mô phỏng vật lý bánh xe:** Sử dụng thành phần `WheelCollider` mặc định của Unity để tính toán độ ma sát của lốp, lực kéo (Motor Torque), lực phanh (Brake Torque) và độ nhún của lò xo giảm chấn.
*   **Cơ chế Hộp số sàn (Manual Transmission):** Người chơi bắt buộc phải sử dụng các phím số chuyên dụng để chuyển đổi cấp số gồm: Số 1, Số 2, Số 3 để di chuyển tiến tùy theo tốc độ; Phím `R` dùng để lùi xe và phím `N` để đưa về số Mo (Neutral) khi dừng xe chờ đèn đỏ hoặc dừng tại vạch đường sắt.
*   **Mô phỏng lực bò (Creep Torque):** Khi xe ở số 1 hoặc số lùi và người chơi nhả phanh, động cơ sẽ cung cấp một lực bò nhỏ để xe chuyển động chậm rãi giống như hoạt động nhả côn thực tế.
*   **Khóa vận tốc chống trôi dốc:** Hiện thực giải thuật khóa cứng vận tốc khi xe dừng hẳn (0 km/h) nhằm hỗ trợ người học dừng xe ngang dốc (đề-pa) mà không bị trôi lùi tự do ngoài ý muốn.

> **[Hình 4.1: Giao diện HUD hiển thị trạng thái động cơ, vận tốc và cấp số sàn (1, 2, 3, N, R) khi xe đang di chuyển]**

### 4.2.2. Module Camera (`CameraController.cs`)
Nhằm hỗ trợ quá trình quan sát tốt nhất cho người học lái xe, hệ thống camera được hiện thực với hai chế độ linh hoạt:
*   **Góc nhìn thứ ba (Third-person view):** Camera được đặt ở phía sau xe và hơi chếch lên cao. Góc nhìn này giúp người học quan sát bao quát toàn bộ thân xe, vệt bánh xe bên phải/trái để căn chỉnh tránh đè vạch chip trên đường.
*   **Góc nhìn thứ nhất (First-person view/Cockpit view):** Camera đặt ở vị trí mắt của người lái bên trong cabin, hiển thị chi tiết vô lăng, bảng táp-lô hiển thị số, vòng tua và tốc độ. Giả lập chân thực cảm giác như đang ngồi sau vô lăng thực tế.
*   **Kỹ thuật mượt hóa:** Sử dụng các thuật toán nội suy tuyến tính `Vector3.Lerp` và nội suy cầu `Quaternion.Slerp` để di chuyển camera mượt mà theo xe, tránh hiện tượng giật giật (stuttering) gây mỏi mắt cho người chơi.

> **[Hình 4.2: So sánh góc nhìn thứ ba (phía sau xe) và góc nhìn thứ nhất (trong cabin buồng lái)]**

### 4.2.3. Module Thi lý thuyết (`TheoryExamManager.cs`)
*   **Nạp câu hỏi thông minh:** Hệ thống tự động đọc và phân tích (parse) danh sách câu hỏi trắc nghiệm B2 từ file `theory_questions.json` khi màn chơi được khởi tạo.
*   **Đếm ngược thời gian:** Thiết lập bộ đếm thời gian thi sát hạch chạy ngược (20 phút). Khi hết giờ hoặc người dùng bấm nộp bài, hệ thống sẽ tự động tổng hợp kết quả.
*   **Chuẩn hóa giao diện phản hồi UI:** Khi người học click chọn câu trả lời, hệ thống áp dụng tông màu xanh lá chủ đạo (`#1FA659`) để làm nổi bật đáp án được chọn, mang lại giao diện hiện đại và trực quan.

> **[Hình 4.3: Giao diện bảng câu hỏi thi lý thuyết B2 với đáp án đang chọn được highlight màu xanh lá #1FA659]**

### 4.2.4. Module Chấm điểm tự động (`ExamManager.cs`)
*   **Quản lý tiến trình thi:** Khởi tạo điểm số ban đầu là 100 điểm. Hệ thống sử dụng mẫu thiết kế Singleton để kiểm soát trạng thái của 11 bài thi liên hoàn trên sa hình.
*   **Cảm biến đè vạch tự động:** Sử dụng các vùng cảm biến va chạm vô hình (`BoxCollider` đặt ở chế độ `isTrigger`) phủ lên các vệt giới hạn, vạch đè chip trên sa hình.
*   **Xử lý trừ điểm và cảnh báo:** Khi xe vi phạm (như đè vạch, tắt máy, dừng xe không đúng vạch quy định), hệ thống sẽ ngay lập tức trừ điểm tương ứng (-5 điểm) và đồng thời phát âm thanh cảnh báo lỗi bằng tiếng Việt thông qua hệ thống Audio. Nếu điểm số giảm xuống dưới 80, hệ thống sẽ dừng bài thi lập tức và công bố kết quả "Thi trượt".

> **[Hình 4.4: Bố trí các vùng cảm biến Collider (Trigger) đè vạch chip trên sơ đồ sa hình trong Unity Editor]**

### 4.2.5. Module Cài đặt hệ thống (`SettingsManager.cs`)
*   **Lưu trữ cấu hình:** Sử dụng lớp `PlayerPrefs` để lưu trữ dữ liệu cấu hình như âm lượng nhạc nền (Music), âm lượng hiệu ứng (SFX), âm lượng giọng hướng dẫn viên (Voice).
*   **Tối ưu hóa đa nền tảng:** Hệ thống tự động nhận dạng thiết bị và môi trường chạy game. Khi phát hiện chạy trên trình duyệt Web (WebGL), hệ thống sẽ tự động ẩn mục tùy chọn "Độ phân giải" và nút "Thoát game" nhằm tối ưu hóa giao diện vừa khít với khung màn hình của itch.io và loại bỏ các lệnh lỗi nền tảng.

> **[Hình 4.5: Giao diện Cài đặt Đồ họa đã tự động ẩn dòng "Độ phân giải" khi giả lập/chạy trên nền tảng WebGL]**

---

## 4.3. Kiểm thử

Quá trình kiểm thử phần mềm được tiến hành xuyên suốt thông qua các phương pháp kiểm thử hộp đen (Black-box testing) và kiểm thử hiệu năng phần cứng nhằm đảm bảo trải nghiệm người dùng tốt nhất.

### 4.3.1. Kiểm thử chức năng (Functional Testing)
Các kịch bản kiểm thử chức năng chính được thực hiện như sau:

| STT | Chức năng kiểm thử | Kịch bản kiểm thử | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | Khởi hành xe | Nhấn SPACE để xuất phát, vào số 1 và giữ phím ga W. | Xe tăng tốc mượt mà từ vị trí xuất phát, điểm số bắt đầu đếm thời gian. | Xe hoạt động đúng theo lực kéo vật lý, âm thanh động cơ tăng dần. | Đạt |
| 2 | Khóa trôi dốc (Đề-pa) | Dừng xe ngang dốc cầu, nhả hết phím ga và phanh. | Xe được khóa chặt ở vận tốc 0 km/h, không bị trôi lùi tự do. | Xe đứng yên vững chắc trên dốc. | Đạt |
| 3 | Cảm biến đè vạch | Lái xe cho bánh đè lên vạch giới hạn màu vàng/đỏ. | Hệ thống trừ 5 điểm và phát âm thanh cảnh báo: "Bánh xe đè vạch". | Trừ điểm chuẩn xác, cập nhật ngay lên HUD. | Đạt |
| 4 | Thi Lý thuyết | Chọn các đáp án và click nút "Nộp bài". | Hệ thống hiển thị bảng kết quả Đạt/Trượt cùng số câu trả lời đúng/sai. | Kết quả hiển thị tức thì, phân loại câu hỏi chuẩn xác. | Đạt |

### 4.3.2. Kiểm thử giao diện (UI Testing)
*   **Kiểm thử độ tương thích màn hình (Responsive):** Thực hiện thay đổi tỷ lệ khung hình game từ 4:3, 16:9, 21:9 và độ phân giải từ HD (1280x720) lên 4K (3840x2160). Kết quả các nút bấm, bảng hiển thị điểm số trên HUD co giãn chuẩn xác, không bị tràn hay đè lên nhau.
*   **Kiểm thử môi trường WebGL:** Chạy thử bản build Web trên nền tảng giả lập WebGL. Các mục không tương thích như nút "Thoát game" và dropdown "Độ phân giải" đã được ẩn đi hoàn toàn, mang lại bố cục UI sạch sẽ và trực quan.

### 4.3.3. Kiểm thử hiệu năng (Performance Testing)
*   **Tốc độ khung hình (Frame Rate):** Tiến hành đo đạc FPS tại các phân cảnh nặng đồ họa (như khu vực hàng đinh và dốc cầu có nhiều cây cối). Game hoạt động ổn định ở mức **60 - 90 FPS** trên cấu hình tầm trung.
*   **Thời gian tải game (Load Time):** Dung lượng bản build WebGL được tối ưu nén tài nguyên xuống còn **~35 MB**, giúp tốc độ load game trên trình duyệt web chỉ mất khoảng **6 - 8 giây** ở tốc độ mạng tiêu chuẩn.

---

## 4.4. Kết quả đạt được

### 4.4.1. Hình ảnh sản phẩm
Dưới đây là một số hình ảnh thực tế ghi lại giao diện hoạt động ổn định của phần mềm **Vietnam B2 Driving Simulator**:

*   **Hình ảnh 4.6:** Giao diện Menu chính tối giản trên nền tảng WebGL (Nút "Thoát" đã được tự động ẩn đi).
*   **Hình ảnh 4.7:** Góc nhìn buồng lái 3D của học viên với các thiết bị đo đạc tốc độ, điểm thi hiện tại và loa thông báo tiếng Việt.
*   **Hình ảnh 4.8:** Bảng thông báo kết quả thi đạt/trượt hiển thị tổng điểm và danh sách lỗi vi phạm chi tiết khi kết thúc bài thi.

### 4.4.2. Đánh giá kết quả
*   **Tính thực tế:** Hệ thống mô phỏng chân thực bài thi sát hạch lái xe B2, bám sát bộ luật chấm điểm của Bộ Giao thông Vận tải Việt Nam giúp ích lớn cho học viên chuẩn bị thi thật.
*   **Tính linh hoạt:** Phần mềm triển khai thành công lên nền tảng **itch.io**, cho phép người dùng mở trình duyệt web lên và chơi trực tiếp mọi lúc mọi nơi mà không cần trải qua các bước tải và cài đặt phức tạp.

---

## 4.5. Hạn chế

Mặc dù dự án đã hoàn thành các mục tiêu đề ra và hoạt động ổn định, hệ thống vẫn tồn tại một số hạn chế kỹ thuật sẽ được cải tiến trong tương lai:
1.  **Chưa hỗ trợ Multiplayer (Chơi nhiều người):** Trò chơi hiện tại chỉ hỗ trợ trải nghiệm chơi đơn (Single-player). Giai đoạn tiếp theo cần nâng cấp hệ thống máy chủ để hỗ trợ nhiều học viên cùng tập lái chung trên một sa hình ảo.
2.  **Thiếu hệ thống AI giao thông (Traffic AI):** Sa hình thi hiện tại chỉ có một mình xe của người học, thiếu vắng các phương tiện AI tự động chạy cắt ngang hoặc người đi bộ qua đường để giả lập các tình huống khẩn cấp bất ngờ.
3.  **Số lượng bản đồ hạn chế:** Hệ thống mới chỉ cung cấp một bản đồ sa hình tiêu chuẩn. Cần mở rộng thêm các môi trường lái xe thực tế ngoài sa hình như đường trường, đường đèo dốc núi cao sương mù, hay lái xe đi trong khu đô thị đông đúc phương tiện qua lại.
