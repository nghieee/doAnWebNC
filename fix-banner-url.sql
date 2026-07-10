-- Sửa nhanh các banner có ImageUrl chỉ là tên file (thiếu /images/banners/ ở đầu)
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio
-- Kết nối tới database LongChau, sau đó chạy:

UPDATE Banners
SET ImageUrl = '/images/banners/' + ImageUrl
WHERE ImageUrl NOT LIKE '/%'
  AND ImageUrl NOT LIKE 'http://%'
  AND ImageUrl NOT LIKE 'https://%'
  AND ImageUrl <> '';

-- Sau khi chạy xong, refresh trang chủ là thấy banner hiển thị.