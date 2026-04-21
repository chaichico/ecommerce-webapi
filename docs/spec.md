# ITSC_EXAM_API_2025
จงพัฒนา Web API ตาม Requirement ดังต่อไปนี้

## Technical Requirement 
1 พัฒนา Web API ด้วย .NET10 และใช้ ฐานข้อมูลชนิดใด ก็ได้ไม่ว่าจะเป็น SQL หรือ NoSQL โดยใช้ Framework ORM ในการเชื่อมต่อ
2 Web API ต้องพัฒนาให้อยู่ในรูปแบบของ REST API ตาม Spec OpenAPI Specification ในปัจจุบัน
3 จงกำหนดค่าตัวแปร ของ Application ด้วย file ENV  และ gitignore   
4 จงเขียน Dockerfile และ เลือกใช้ Base Image ที่เหมาะสมและ กิน resource น้อย 
5 จงเขียน Docker-compose เพื่อ Run ระบบ โดย กำหนดให้ Web API  Run ที่Port  8080  
6 เมื่อ Login แล้ว ให้สร้าง JWT token สำหรับ User  ในการทำ api Authentication and Authorization ของ API ที่ Required การauthentication and authorization 


## Optional Technical Requirement คะแนนพิเศษเพิ่มจากคะแนนเต็มซึ่งไม่ทำก็ได้
1 Web API มีโครงสร้าง Code รองรับการ Run Automate Test   บน CI/CD และ Database เป็น Inmemmory เมื่อ กำหนด ตัวแปร ENV RunMode = test
2 ใช้ Code First ในการพัฒนา และ มีการทำ Database Migration


## Business Requirement 
บริษัท ต้องการพัฒนาระบบ Backend สำหรับจัดการคลังสินค้าและคำสั่งซื้อ (Order Management System) สำหรับร้านค้า E-commerce ขนาดเล็กที่ต้องการความรวดเร็วและประสิทธิภาพสูง จงพัฒนา Web API เพื่อรองรับฟังก์ชันการทำงานหลักของระบบนี้"

1 มี Function Seed Data Product Item และ Seed Data Product Status Reference  แบบ Auto ในกรณีที่ Data collection เป็น empty 

2 มีFunction ลงทะเบียน Create User Account (API Create User)    
    2.1 Function Create User Account  มี Data ที่ต้องกรอกคือ
        -Email(คือ User account ไม่สามารถซ้ำได้ แต่ไม่ใช่ Primary Key)
        - ชื่อ
        - นาม สกุล 
        - เบอร์โทร(Optional เข้ารหัสด้วย Symmetric Encryption ได้คะแนนพิเศษ)
        - Password (Optional เข้ารหัสด้วย Hashing Encryption ได้คะแนนพิเศษ)
        - ConfirmPassword (Optional เข้ารหัสด้วย Hashing Encryption ได้คะแนนพิเศษ)
3 มีFunction User Login (API User Login)  
    3.1 User สามารถ login ด้วย User(Email) และ Password
    3.2 API return User Basic Info คือ Email,ชื่อ, นาม สกุล, อยู่ใน Payload ของ JWT Token

4 มี Function จัดการสินค้า ของ Admin (API Admin Search Order List)) (API Authentication ด้วยการ  Basic Authentication  กำหนด ตัวแปร User & password นี้ที่ ENV)(10คะแนน) 
    4.1 สามารถค้นหา เลขคำสั่งซื้อสินค้า  OrderNumber,ชื่อ นาม สกุล ผู้สั่ง
    4.2 Data Return Order Product List พร้อมสถาณะคำสั่งซื้อ { รอยืนยันคำสั่งซื้อ , ยืนยันคำสั่งซื้อ }  แต่ละ Order มี data Order Detail<Order> 

5 มี Function เพิ่มคำสั่งซื้อสินค้า (API User Create Order)( API request JWT สำหรับ Authentication and Authorization )  
    4.1 Data ที่ต้อง Add Order Detail โดย 1 คำสั่งซื้อสามารถ เลือกสินได้หลายอย่าง แต่ละอย่างมีจำนวนแตกต่างกัน และ return OrderNumber โดย Order Detail         		ประกอบด้วย
- Product Number 
- จำนวน item

6 มี Function แก้ไข คำสั่งซื้อสินค้า (API User Update Order)( API request JWT สำหรับ Authentication and Authorization )   
    6.1 Data ที่ต้อง แก้ไขได้ ตาม คำสั่งซื้อ OrderNumber
-Product Number 
- จำนวน

7 มี Function User ยืนยัน สั่งซื้อสินค้า (API User Confirm Order)( API request JWT สำหรับ Authentication and Authorization )  (5คะแนน) 
    7.1 Data ที่ต้อง Put ซึ่งต้องตามคำสั่งซื้อOrderNumber 
-ที่อยู่จัดส่ง

8 มี Function Admin  update สถาณะคำสั่งซื้อ (API Admin Approve Order ) (API Authentication ด้วยการ  Basic Authentication กำหนด ตัวแปร User & password นี้ที่ ENV)  
    8.1 Admin สามารถ select เลือกยืนยัน user Order  ได้หลายOrderพร้อมกัน เพื่อเปลี่ยน สถาณะ เป็น  ยืนยันคำสั่งซื้อ

