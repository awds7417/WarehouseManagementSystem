# Warehouse Management System（倉儲管理系統）

ASP.NET MVC + SQL Server + ADO.NET 實作的倉儲管理 Web 系統。  
包含產品 / 客戶主檔、進出貨紀錄、庫存現況查詢、登入與權限控管等功能，  
主要目的是作為實務開發與面試作品展示。

---

## 🔍 專案簡介

- 自行從零設計與實作的倉儲管理系統
- 技術棧：
  - ASP.NET MVC（.NET Framework 4.5）
  - C#
  - ADO.NET（SqlConnection / SqlCommand / SqlDataReader）
  - SQL Server / SQL Express
  - HTML / Razor View
- 主要功能：
  - 產品主檔管理（CRUD + 關鍵字搜尋 + 分頁）
  - 客戶主檔管理（CRUD + 分頁）
  - 進出貨紀錄（IN / OUT）新增 / 列表 / 刪除 + 分頁
  - 庫存現況查詢（TotalIn / TotalOut / CurrentStock）+ CSV 匯出
  - 登入 / 登出、角色權限（Admin / 一般使用者）
  - 防止負庫存的交易與鎖定機制

---

## 🧩 系統架構（Architecture）

### 分層設計

- **Presentation Layer（Views）**
  - Razor View（.cshtml）
  - 負責產生 HTML、顯示表格與表單
- **Controller Layer**
  - ASP.NET MVC Controllers
  - 負責處理 HTTP 請求、驗證輸入、呼叫 Repository 取得 / 更新資料
- **Model / ViewModel Layer**
  - Entity Models：`Product`, `Customer`, `InventoryTransaction`, `InventorySummary`, `User`…
  - ViewModels：`ProductListViewModel`, `CustomerListViewModel`, `InventoryStatusListViewModel`, `InventoryTransactionListViewModel`…
  - 將資料物件與畫面需求分開
- **Repository Layer**
  - `ProductRepository`, `CustomerRepository`, `InventoryTransactionRepository`, `InventoryReportRepository`, `UserRepository`
  - 使用 ADO.NET 直接操作 SQL Server
  - 統一管理 SQL 語法與連線
- **Database**
  - SQL Server：`WarehouseDb`
  - 主要資料表：
    - `Products`
    - `Customers`
    - `InventoryTransactions`
    - `Users`

---

## 🗄 資料庫設計（概要）

### Products

- `Id` (int, PK)
- `ProductCode` (nvarchar) – 產品代號
- `Name` (nvarchar) – 產品名稱
- `Unit` (nvarchar) – 單位（PCS、BOX…）
- `SafeStockQty` (int) – 安全庫存

### Customers

- `Id` (int, PK)
- `CustomerCode` (nvarchar)
- `Name` (nvarchar)
- `ContactPerson` (nvarchar)
- `Phone` (nvarchar)
- `Address` (nvarchar)

### InventoryTransactions

- `Id` (int, PK)
- `ProductId` (int, FK -> Products.Id)
- `CustomerId` (int, FK -> Customers.Id, 可為 NULL)
- `TxType` (char) – `'IN'` / `'OUT'`
- `Quantity` (int)
- `TxDate` (datetime)
- `Remark` (nvarchar)

> 不另外存「庫存表」，而是以 `InventoryTransactions` 之交易明細  
> 搭配 SQL `SUM + GROUP BY` 即時計算 TotalIn / TotalOut / CurrentStock。

### Users

- `Id` (int, PK)
- `UserName` (nvarchar)
- `PasswordHash` (nvarchar) – 使用 SHA256 雜湊
- `DisplayName` (nvarchar)
- `IsAdmin` (bit)

---

## 🔐 登入與權限控管

- 使用 **FormsAuthentication**：
  - Web.config：`<authentication mode="Forms">` + `<deny users="?" />`
  - 未登入的使用者導向 Login 頁面
- `AccountController.Login`：
  - 透過 `UserRepository` 查詢使用者
  - 使用 `PasswordHelper` 以 SHA256 產生雜湊，與 `PasswordHash` 比對
  - 登入成功後呼叫 `FormsAuthentication.SetAuthCookie`
  - 將 `DisplayName` 與 `IsAdmin` 存入 `Session`
- 授權：
  - `[Authorize]` / `[AllowAnonymous]` 控制動作是否需登入
  - 部分功能檢查 `Session["IsAdmin"]`，僅管理者可使用

---

## 🔄 進出貨流程與防負庫存機制

1. 使用者在「庫存現況」頁面點選某產品的「入庫 / 出庫」
2. 導向 `InventoryTransactionsController.Create`，填寫數量、客戶、備註
3. 送出表單後，由 `InventoryTransactionRepository.Insert` 處理：
   - 使用 `SqlTransaction` 開啟交易
   - 以 `WITH (UPDLOCK, HOLDLOCK)` 查詢該產品目前庫存（CurrentStock）
   - 若為出庫且 `CurrentStock < Quantity`：
     - Rollback，回傳「庫存不足」錯誤
   - 否則：
     - 寫入一筆新的 `InventoryTransactions` 記錄
     - Commit Transaction
4. 避免併發出庫造成負庫存，確保資料一致性

---

## 📊 分頁、查詢與報表

- 產品 / 客戶 / 進出貨 / 庫存現況列表皆支援分頁：
  - 以 SQL Server `ROW_NUMBER()` + ViewModel 實作
  - 每頁筆數可調整（目前範例為 6 筆）
- 產品列表支援關鍵字搜尋（依代號 / 名稱）
- 庫存現況支援 CSV 匯出：
  - Controller 呼叫 `InventoryReportRepository.GetInventorySummary()`
  - 將結果輸出為 CSV 檔供管理者下載

---

## 🛠 如何在本機執行（Getting Started）

### 1. 環境需求

- Windows + Visual Studio（例如 VS 2013）
- .NET Framework 4.5
- SQL Server / SQL Express（本機或遠端皆可）

### 2. 建立資料庫

1. 在 SQL Server 建立一個新的資料庫：`WarehouseDb`
2. 執行專案提供的 SQL Script（若有）建立：
   - `Products`
   - `Customers`
   - `InventoryTransactions`
   - `Users`
3. 可先插入一些測試資料（產品、客戶、測試帳號）

### 3. 設定連線字串

在 `Web.config` 中設定：

```xml
<connectionStrings>
  <add name="WarehouseDb"
       connectionString="Data Source=localhost\SQLEXPRESS;Initial Catalog=WarehouseDb;Integrated Security=True;MultipleActiveResultSets=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
