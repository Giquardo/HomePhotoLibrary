-- Query 1: Get all details of all photos
SELECT * FROM photoalbumdatabase.photos;

-- Query 2: Get all details of all albums
SELECT * FROM photoalbumdatabase.albums;

-- Query 3: Get details of all photos along with their album details
SELECT 
    p.Id as photo_id, 
    a.Title as album_title, 
    p.Title as photo_title, 
    p.DateUploaded as timestamp, 
    p.Description as photo_description, 
    p.Extension as file_extension, 
    p.FilePath as file_path, 
    p.Url as url, 
    p.Hash as hash
FROM 
    photoalbumdatabase.photos p
JOIN 
    photoalbumdatabase.albums a 
ON 
    p.AlbumId = a.Id;

-- Query 4: Get a count of all pictures in each album
SELECT 
    a.Title as album_title, 
    COUNT(p.Id) as photo_count
FROM 
    photoalbumdatabase.albums a
LEFT JOIN 
    photoalbumdatabase.photos p 
ON 
    a.Id = p.AlbumId
GROUP BY 
    a.Title;

-- Query 5: Get a count of all pictures in each album with at least 1 picture
SELECT 
    a.Title as album_title, 
    COUNT(p.Id) as photo_count
FROM 
    photoalbumdatabase.albums a
INNER JOIN 
    photoalbumdatabase.photos p 
ON 
    a.Id = p.AlbumId
GROUP BY 
    a.Title;

-- Query 6: List all photos with their album names and count of photos in each album
SELECT 
    p.Id as photo_id, 
    a.Title as album_title, 
    p.Title as photo_title, 
    p.DateUploaded as timestamp, 
    p.Description as photo_description, 
    p.Extension as file_extension, 
    p.FilePath as file_path, 
    p.Url as url, 
    p.Hash as hash,
    (SELECT COUNT(*) FROM photoalbumdatabase.photos WHERE AlbumId = a.Id) as photo_count
FROM 
    photoalbumdatabase.photos p
JOIN 
    photoalbumdatabase.albums a 
ON 
    p.AlbumId = a.Id;

-- Query 7: Find the most recent photo in each album
SELECT 
    a.Title as album_title, 
    p.Title as most_recent_photo,
    p.DateUploaded as upload_date
FROM 
    photoalbumdatabase.photos p
JOIN 
    photoalbumdatabase.albums a 
ON 
    p.AlbumId = a.Id
WHERE 
    p.DateUploaded = (
        SELECT MAX(DateUploaded)
        FROM photoalbumdatabase.photos
        WHERE AlbumId = a.Id
    );

-- Query 8: Count the total number of photos across all albums
SELECT 
    COUNT(*) as total_photos
FROM 
    photoalbumdatabase.photos;

-- Query 9: Get the list of albums with no photos
SELECT 
    a.Title as album_title
FROM 
    photoalbumdatabase.albums a
LEFT JOIN 
    photoalbumdatabase.photos p 
ON 
    a.Id = p.AlbumId
WHERE 
    p.Id IS NULL;

-- Query 10: Get the average number of photos per album
SELECT 
    AVG(photo_count) as average_photos_per_album
FROM (
    SELECT 
        COUNT(p.Id) as photo_count
    FROM 
        photoalbumdatabase.albums a
    LEFT JOIN 
        photoalbumdatabase.photos p 
    ON 
        a.Id = p.AlbumId
    GROUP BY 
        a.Id
) as album_photo_counts;

-- Query 11: List all photos uploaded in the last 30 days
SELECT 
    p.Id as photo_id, 
    a.Title as album_title, 
    p.Title as photo_title, 
    p.DateUploaded as timestamp, 
    p.Description as photo_description, 
    p.Extension as file_extension, 
    p.FilePath as file_path, 
    p.Url as url, 
    p.Hash as hash
FROM 
    photoalbumdatabase.photos p
JOIN 
    photoalbumdatabase.albums a 
ON 
    p.AlbumId = a.Id
WHERE 
    p.DateUploaded >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);

-- Query 12: List all photos of a specific album
SELECT 
    p.Id as photo_id, 
    p.Title as photo_title, 
    p.DateUploaded as timestamp, 
    p.Description as photo_description, 
    p.Extension as file_extension, 
    p.FilePath as file_path, 
    p.Url as url, 
    p.Hash as hash
FROM 
    photoalbumdatabase.photos p
JOIN 
    photoalbumdatabase.albums a 
ON 
    p.AlbumId = a.Id
WHERE 
    a.Title = 'Nature';  -- Change 'Nature' to the album title you want to query

-- Query 13: List all albums and the total size of all photos in each album
SELECT 
    a.Title as album_title, 
    COUNT(p.Id) as photo_count,
    SUM(LENGTH(p.FilePath)) as total_size
FROM 
    photoalbumdatabase.albums a
LEFT JOIN 
    photoalbumdatabase.photos p 
ON 
    a.Id = p.AlbumId
GROUP BY 
    a.Id;
