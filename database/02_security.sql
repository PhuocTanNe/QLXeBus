-- =====================================================================
-- Bảng bảo mật B11..B15 + thuộc tính kiểm soát (báo cáo, mục 3.2)
-- =====================================================================

CREATE TABLE IF NOT EXISTS n_ngdung (               -- B11
    idnhom  varchar(10)  PRIMARY KEY,
    tennhom varchar(100) NOT NULL,
    mota    varchar(500)
);

CREATE TABLE IF NOT EXISTS quyen (                  -- B12
    idquyen     varchar(10)  PRIMARY KEY,
    tenquyen    varchar(150) NOT NULL,
    machucnang  varchar(30)  NOT NULL UNIQUE,       -- mã chức năng, ứng dụng kiểm tra theo mã này
    loaithaotac char(1)      NOT NULL CHECK (loaithaotac IN ('C','E','D','R')),  -- Thêm/Sửa/Xóa/Xem
    mota        varchar(500)
);

CREATE TABLE IF NOT EXISTS quyen_nnd (              -- B11.1
    idnhom  varchar(10) NOT NULL REFERENCES n_ngdung(idnhom) ON DELETE CASCADE,
    idquyen varchar(10) NOT NULL REFERENCES quyen(idquyen)   ON DELETE CASCADE,
    PRIMARY KEY (idnhom, idquyen)
);

CREATE TABLE IF NOT EXISTS nguoidung (              -- B13
    idndung   varchar(10)  PRIMARY KEY,
    tenndung  varchar(100) NOT NULL,
    chucvu    varchar(100),
    donvi     varchar(100),
    tendnhap  varchar(50)  NOT NULL UNIQUE,
    matkhau   varchar(200) NOT NULL,                -- băm PBKDF2 (không lưu mật khẩu thô)
    idnhom    varchar(10)  NOT NULL REFERENCES n_ngdung(idnhom),
    trangthai varchar(20)  NOT NULL DEFAULT 'Hoạt động'
);

CREATE TABLE IF NOT EXISTS ls_dnhap (               -- B14
    idlsdn     serial PRIMARY KEY,
    idndung    varchar(10) REFERENCES nguoidung(idndung) ON DELETE SET NULL,
    tendnhap   varchar(50),                          -- lưu thêm để vẫn truy vết khi đăng nhập sai tên
    tgdangnhap timestamp NOT NULL DEFAULT now(),
    tgdangxuat timestamp,
    diachiip   varchar(45),
    tenthietbi varchar(100),
    ketqua     varchar(20) NOT NULL                  -- Thành công | Thất bại
);

CREATE TABLE IF NOT EXISTS saoluu (                 -- B15
    idsaoluu  serial PRIMARY KEY,
    idndung   varchar(10) REFERENCES nguoidung(idndung) ON DELETE SET NULL,
    thoigian  timestamp NOT NULL DEFAULT now(),
    filepath  varchar(500),
    kichthuoc bigint,
    loaisaoluu varchar(20) NOT NULL DEFAULT 'Thủ công'  -- Thủ công | Tự động
);

-- 3.2.2 Thuộc tính kiểm soát
ALTER TABLE tuyenbuyt ADD COLUMN IF NOT EXISTS idntao  varchar(10) REFERENCES nguoidung(idndung) ON DELETE SET NULL;
ALTER TABLE tuyenbuyt ADD COLUMN IF NOT EXISTS tenntao varchar(100);
ALTER TABLE trambuyt  ADD COLUMN IF NOT EXISTS idntao  varchar(10) REFERENCES nguoidung(idndung) ON DELETE SET NULL;
ALTER TABLE trambuyt  ADD COLUMN IF NOT EXISTS tenntao varchar(100);

-- ---------------- Dữ liệu danh mục: nhóm & quyền ----------------
INSERT INTO n_ngdung(idnhom, tennhom, mota) VALUES
 ('NQL','Người quản lý','Bộ phận quản lý: tra cứu, cập nhật, phân tích, thống kê'),
 ('KH', 'Khách hàng','Bộ phận khách hàng: tra cứu trạm, tuyến'),
 ('QTV','Quản trị viên','Quản trị người dùng, phân quyền, lịch sử truy cập, sao lưu')
ON CONFLICT DO NOTHING;

INSERT INTO quyen(idquyen, tenquyen, machucnang, loaithaotac) VALUES
 ('Q01','Xem thông tin trạm',                    'TRAM_XEM',        'R'),
 ('Q02','Tra cứu trạm theo thuộc tính',          'TRAM_TC_TT',      'R'),
 ('Q03','Tra cứu trạm theo không gian',          'TRAM_TC_KG',      'R'),
 ('Q04','Cập nhật thông tin thuộc tính trạm',    'TRAM_SUA',        'E'),
 ('Q05','Xem thông tin tuyến',                   'TUYEN_XEM',       'R'),
 ('Q06','Tra cứu tuyến theo thuộc tính',         'TUYEN_TC_TT',     'R'),
 ('Q07','Tra cứu tuyến theo không gian',         'TUYEN_TC_KG',     'R'),
 ('Q08','Cập nhật thông tin thuộc tính tuyến',   'TUYEN_SUA',       'E'),
 ('Q09','Gán trạm vào tuyến xe buýt',            'TUYEN_GAN_TRAM',  'E'),
 ('Q10','Chọn vùng cần phân tích',               'DX_VUNG',         'R'),
 ('Q11','Xác định các khu vực thiếu trạm',       'DX_THIEU',        'R'),
 ('Q12','Xác định vị trí ứng viên trạm',         'DX_UNGVIEN',      'R'),
 ('Q13','So sánh mức độ tối ưu của các ứng viên','DX_SOSANH',       'R'),
 ('Q14','Đề xuất vị trí trạm xe tối ưu nhất',    'DX_DEXUAT',       'C'),
 ('Q15','Thống kê số lượng trạm theo khu vực',   'TK_KHUVUC',       'R'),
 ('Q16','Thống kê trạm theo tuyến xe buýt',      'TK_TUYEN',        'R'),
 ('Q17','Thống kê phạm vi phục vụ của trạm',     'TK_PHAMVI',       'R'),
 ('Q18','Xuất file báo cáo',                     'TK_XUATFILE',     'R'),
 ('Q19','Thêm mới người dùng nghiệp vụ',         'QT_THEM_ND',      'C'),
 ('Q20','Chỉnh sửa thông tin người dùng',        'QT_SUA_ND',       'E'),
 ('Q21','Xóa người dùng nghiệp vụ',              'QT_XOA_ND',       'D'),
 ('Q22','Phân quyền người dùng',                 'QT_PHANQUYEN',    'E'),
 ('Q23','Xem lịch sử truy cập',                  'QT_LICHSU',       'R'),
 ('Q24','Sao lưu dữ liệu hệ thống',              'QT_SAOLUU',       'C')
ON CONFLICT DO NOTHING;

-- Người quản lý: Q01..Q18
INSERT INTO quyen_nnd(idnhom, idquyen)
SELECT 'NQL', idquyen FROM quyen WHERE idquyen BETWEEN 'Q01' AND 'Q18' ON CONFLICT DO NOTHING;
-- Khách hàng: Q01..Q07
INSERT INTO quyen_nnd(idnhom, idquyen)
SELECT 'KH', idquyen FROM quyen WHERE idquyen BETWEEN 'Q01' AND 'Q07' ON CONFLICT DO NOTHING;
-- Quản trị viên: Q19..Q24
INSERT INTO quyen_nnd(idnhom, idquyen)
SELECT 'QTV', idquyen FROM quyen WHERE idquyen BETWEEN 'Q19' AND 'Q24' ON CONFLICT DO NOTHING;

-- Tài khoản mặc định được ứng dụng tạo ở lần đăng nhập đầu tiên (xem README) vì cần băm mật khẩu.
