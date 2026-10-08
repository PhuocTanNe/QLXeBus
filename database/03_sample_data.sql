-- Dữ liệu MẪU minh hoạ (toạ độ gần đúng, số liệu giả định) để chạy thử ứng dụng.
-- Thay bằng dữ liệu thật của Hà Nội khi triển khai. Chạy sau 01_schema.sql và 02_security.sql.

INSERT INTO quanhuyen VALUES
 ('Q001','Hoàn Kiếm'),('Q002','Ba Đình'),('Q003','Đống Đa'),('Q004','Cầu Giấy'),('Q005','Hai Bà Trưng')
ON CONFLICT DO NOTHING;

INSERT INTO phuongxa VALUES
 ('P001','Q001','Hàng Bài'),('P002','Q001','Cửa Nam'),
 ('P003','Q002','Điện Biên'),('P004','Q002','Kim Mã'),
 ('P005','Q003','Thịnh Quang'),('P006','Q003','Ngã Tư Sở'),
 ('P007','Q004','Dịch Vọng'),('P008','Q004','Mai Dịch'),('P009','Q004','Trung Hòa'),
 ('P010','Q005','Bách Khoa')
ON CONFLICT DO NOTHING;

INSERT INTO tuyenbuyt(matuyen, tentuyen, mota, trangthai) VALUES
 ('01','Bờ Hồ - Bách Khoa','Tuyến mẫu nội thành hướng Nam','Hoạt động'),
 ('02','Lăng Bác - ĐH Quốc gia','Tuyến mẫu hướng Tây','Hoạt động'),
 ('03','Ngã Tư Sở - Thái Hà','Tuyến mẫu Đống Đa','Hoạt động')
ON CONFLICT DO NOTHING;

INSERT INTO doanduong VALUES
 ('D001','01','P001','Phố Huế',        'Đường phố',1,1.200,6,'Hoạt động'),
 ('D002','01','P002','Đại Cồ Việt',    'Đường chính',2,1.800,8,'Hoạt động'),
 ('D003','02','P003','Điện Biên Phủ',  'Đường chính',1,2.500,10,'Hoạt động'),
 ('D004','02','P007','Xuân Thủy',      'Đường chính',2,3.000,12,'Hoạt động'),
 ('D005','03','P006','Nguyễn Trãi',    'Đường chính',1,1.500,7,'Hoạt động')
ON CONFLICT DO NOTHING;

INSERT INTO loaitram VALUES
 ('LT01','Cột biển báo','Điểm dừng chỉ có biển'),
 ('LT02','Nhà chờ','Điểm dừng có mái che'),
 ('LT03','Bến đầu cuối','Điểm đầu/cuối tuyến')
ON CONFLICT DO NOTHING;

INSERT INTO phuongtien VALUES
 ('PT01','29B-10001','Xe buýt 40 chỗ',40,'Hoạt động'),
 ('PT02','29B-10002','Xe buýt 60 chỗ',60,'Hoạt động'),
 ('PT03','29B-10003','Xe buýt điện',45,'Hoạt động')
ON CONFLICT DO NOTHING;

INSERT INTO trambuyt(matram,matuyen,maloaitram,madoan,maphuongtien,tentram,vido,kinhdo,trangthai,thutu) VALUES
 ('T0001','01','LT03','D001','PT01','Bờ Hồ',            21.0292,105.8527,'Hoạt động',1),
 ('T0002','01','LT02','D001','PT01','Hàng Bài',         21.0245,105.8531,'Hoạt động',2),
 ('T0003','01','LT01','D002','PT01','Nguyễn Du',        21.0189,105.8497,'Hoạt động',3),
 ('T0004','01','LT02','D002','PT01','Đại Cồ Việt',      21.0058,105.8476,'Hoạt động',4),
 ('T0005','01','LT03','D002','PT01','ĐH Bách Khoa',     21.0054,105.8430,'Hoạt động',5),
 ('T0006','02','LT03','D003','PT02','Lăng Bác',         21.0368,105.8346,'Hoạt động',1),
 ('T0007','02','LT02','D003','PT02','Kim Mã',           21.0306,105.8162,'Hoạt động',2),
 ('T0008','02','LT01','D003','PT02','Nguyễn Chí Thanh', 21.0235,105.8105,'Hoạt động',3),
 ('T0009','02','LT02','D004','PT02','Cầu Giấy',         21.0333,105.7945,'Hoạt động',4),
 ('T0010','02','LT03','D004','PT02','ĐH Quốc gia',      21.0378,105.7826,'Hoạt động',5),
 ('T0011','03','LT03','D005','PT03','Ngã Tư Sở',        20.9993,105.8195,'Hoạt động',1),
 ('T0012','03','LT01','D005','PT03','Thái Hà',          21.0113,105.8205,'Hoạt động',2),
 ('T0013','03','LT03','D005','PT03','Chùa Bộc',         21.0087,105.8269,'Hoạt động',3)
ON CONFLICT DO NOTHING;

INSERT INTO khudancu(makhu,maphuong,tenkhu,vido,kinhdo,danso,matdodancu) VALUES
 ('K001','P001','KDC Hàng Bài',        21.0250,105.8520,  9000,34000),
 ('K002','P002','KDC Cửa Nam',         21.0210,105.8440,  8500,30000),
 ('K003','P003','KDC Điện Biên',       21.0350,105.8380, 11000,28000),
 ('K004','P004','KDC Kim Mã',          21.0330,105.8190, 12500,26000),
 ('K005','P004','KDC Vạn Phúc',        21.0310,105.8110,  7000,20000),
 ('K006','P007','KDC Dịch Vọng',       21.0380,105.7930, 15000,22000),
 ('K007','P008','KDC Mai Dịch',        21.0420,105.7740, 13000,18000),
 ('K008','P009','KDC Trung Hòa',       21.0110,105.7990, 16000,25000),
 ('K009','P006','KDC Ngã Tư Sở',       20.9990,105.8170, 14000,32000),
 ('K010','P005','KDC Thịnh Quang',     21.0040,105.8130, 10500,29000),
 ('K011','P010','KDC Bách Khoa',       21.0030,105.8480, 12000,31000),
 ('K012','P010','KDC Tương Mai',       20.9890,105.8550, 18000,35000),
 ('K013','P005','KDC Láng Hạ',         21.0150,105.8110,  9500,27000),
 ('K014','P009','KDC Nhân Chính',      21.0040,105.8040, 14500,26000)
ON CONFLICT DO NOTHING;

INSERT INTO loaicongtrinh VALUES
 ('LC01','Trường học','Trường phổ thông/đại học',1.50),
 ('LC02','Bệnh viện','Cơ sở y tế',2.00),
 ('LC03','Chợ - Trung tâm thương mại','',1.20),
 ('LC04','Cơ quan hành chính','',1.00)
ON CONFLICT DO NOTHING;

INSERT INTO congtrinh(macongtrinh,maphuong,maloaicongtrinh,tencongtrinh,diachi,vido,kinhdo,quymo,mucdouutien) VALUES
 ('C001','P008','LC01','Trường THPT mẫu Mai Dịch','Mai Dịch, Cầu Giấy',21.0410,105.7770,1800,2),
 ('C002','P009','LC02','Bệnh viện mẫu Trung Hòa',  'Trung Hòa, Cầu Giấy',21.0120,105.8010, 500,1),
 ('C003','P005','LC03','Chợ mẫu Thịnh Quang',       'Thịnh Quang, Đống Đa',21.0060,105.8140,  0,3),
 ('C004','P003','LC04','UBND mẫu Điện Biên',        'Điện Biên, Ba Đình', 21.0340,105.8370,  0,3),
 ('C005','P010','LC01','Trường ĐH mẫu Bách Khoa',   'Bách Khoa, Hai Bà Trưng',21.0045,105.8450,25000,1),
 ('C006','P006','LC02','Bệnh viện mẫu Ngã Tư Sở',   'Ngã Tư Sở, Đống Đa', 21.0000,105.8210,  800,1),
 ('C007','P009','LC01','Trường THCS mẫu Nhân Chính', 'Nhân Chính',          21.0050,105.8030,1200,2),
 ('C008','P010','LC03','Chợ mẫu Tương Mai',          'Tương Mai',           20.9880,105.8560,   0,3)
ON CONFLICT DO NOTHING;

-- Phạm vi phục vụ ban đầu (bán kính 500 m) được ứng dụng tính lại bằng chức năng
-- "Thống kê > Cập nhật phạm vi phục vụ".
