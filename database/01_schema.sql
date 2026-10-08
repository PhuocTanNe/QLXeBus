-- =====================================================================
-- QLXeBus - Hệ thống GIS hỗ trợ quản lý, phân tích, thiết lập trạm xe buýt
-- B01..B10 + B06.1, B06.2 : bảng nghiệp vụ (theo báo cáo, mục 2.2.5)
-- CSDL: HQTCSDL (PostgreSQL). Không bắt buộc PostGIS: toạ độ lưu viDo/kinhDo (WGS84).
-- Chạy file này trên DB HQTCSDL (pgAdmin4 > Query Tool).
-- =====================================================================

CREATE TABLE IF NOT EXISTS tuyenbuyt (
    matuyen     varchar(10)  PRIMARY KEY,
    tentuyen    varchar(100) NOT NULL,
    mota        varchar(500),
    trangthai   varchar(20)  NOT NULL DEFAULT 'Hoạt động',
    ngaytao     timestamp    NOT NULL DEFAULT now(),
    ngaycapnhat timestamp    NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS quanhuyen (
    maquan  varchar(10)  PRIMARY KEY,
    tenquan varchar(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS phuongxa (
    maphuong  varchar(10)  PRIMARY KEY,
    maquan    varchar(10)  NOT NULL REFERENCES quanhuyen(maquan),
    tenphuong varchar(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS doanduong (
    madoan     varchar(10)  PRIMARY KEY,
    matuyen    varchar(10)  NOT NULL REFERENCES tuyenbuyt(matuyen),
    maphuong   varchar(10)  NOT NULL REFERENCES phuongxa(maphuong),
    tenduong   varchar(100) NOT NULL,
    loaiduong  varchar(50),
    thutu      smallint,
    chieudaikm numeric(10,3),
    tgdichuyen numeric(8,2),
    trangthai  varchar(20) NOT NULL DEFAULT 'Hoạt động'
);

CREATE TABLE IF NOT EXISTS loaitram (
    maloaitram  varchar(10)  PRIMARY KEY,
    tenloaitram varchar(100) NOT NULL,
    mota        varchar(500)
);

CREATE TABLE IF NOT EXISTS phuongtien (
    maphuongtien   varchar(10) PRIMARY KEY,
    bienso         varchar(15),
    loaiphuongtien varchar(50),
    succhua        smallint,
    trangthai      varchar(20) NOT NULL DEFAULT 'Hoạt động'
);

-- Trạm: matuyen/madoan/maphuongtien cho phép NULL vì trạm "Đề xuất" chưa gán tuyến.
-- thutu = thứ tự phục vụ của trạm trên tuyến (bổ sung so với báo cáo, cần cho chức năng "Gán trạm vào tuyến").
CREATE TABLE IF NOT EXISTS trambuyt (
    matram       varchar(10)   PRIMARY KEY,
    matuyen      varchar(10)   REFERENCES tuyenbuyt(matuyen),
    maloaitram   varchar(10)   REFERENCES loaitram(maloaitram),
    madoan       varchar(10)   REFERENCES doanduong(madoan),
    maphuongtien varchar(10)   REFERENCES phuongtien(maphuongtien),
    tentram      varchar(150)  NOT NULL,
    vido         numeric(10,7) NOT NULL CHECK (vido  BETWEEN -90  AND 90),
    kinhdo       numeric(10,7) NOT NULL CHECK (kinhdo BETWEEN -180 AND 180),
    trangthai    varchar(20)   NOT NULL DEFAULT 'Hoạt động',   -- Hoạt động | Đề xuất | Ngừng hoạt động
    thutu        smallint,
    ngaytao      timestamp     NOT NULL DEFAULT now(),
    ngaycapnhat  timestamp     NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_trambuyt_tuyen ON trambuyt(matuyen, thutu);
CREATE INDEX IF NOT EXISTS ix_trambuyt_xy    ON trambuyt(vido, kinhdo);

CREATE TABLE IF NOT EXISTS khudancu (
    makhu      varchar(10)   PRIMARY KEY,
    maphuong   varchar(10)   NOT NULL REFERENCES phuongxa(maphuong),
    tenkhu     varchar(150)  NOT NULL,
    vido       numeric(10,7) NOT NULL,
    kinhdo     numeric(10,7) NOT NULL,
    danso      integer       NOT NULL DEFAULT 0,
    matdodancu numeric(10,2),
    trangthai  varchar(20)   NOT NULL DEFAULT 'Hoạt động'
);

CREATE TABLE IF NOT EXISTS loaicongtrinh (
    maloaicongtrinh  varchar(10)  PRIMARY KEY,
    tenloaicongtrinh varchar(100) NOT NULL,
    mota             varchar(500),
    trongso          numeric(4,2) NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS congtrinh (
    macongtrinh     varchar(10)   PRIMARY KEY,
    maphuong        varchar(10)   NOT NULL REFERENCES phuongxa(maphuong),
    maloaicongtrinh varchar(10)   NOT NULL REFERENCES loaicongtrinh(maloaicongtrinh),
    tencongtrinh    varchar(150)  NOT NULL,
    diachi          varchar(200),
    vido            numeric(10,7) NOT NULL,
    kinhdo          numeric(10,7) NOT NULL,
    quymo           integer,
    mucdouutien     smallint,
    trangthai       varchar(20)   NOT NULL DEFAULT 'Hoạt động'
);

-- B06.1 / B06.2: phạm vi phục vụ của trạm (quan hệ nhiều-nhiều)
CREATE TABLE IF NOT EXISTS ctpvkhudancu (
    matram      varchar(10) NOT NULL REFERENCES trambuyt(matram) ON DELETE CASCADE,
    makhu       varchar(10) NOT NULL REFERENCES khudancu(makhu)  ON DELETE CASCADE,
    khoangcachm numeric(10,2),
    PRIMARY KEY (matram, makhu)
);

CREATE TABLE IF NOT EXISTS ctpvcongtrinh (
    matram      varchar(10) NOT NULL REFERENCES trambuyt(matram)     ON DELETE CASCADE,
    macongtrinh varchar(10) NOT NULL REFERENCES congtrinh(macongtrinh) ON DELETE CASCADE,
    khoangcachm numeric(10,2),
    PRIMARY KEY (matram, macongtrinh)
);
