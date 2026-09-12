USE ClinicManagementSystem;
GO

-- ============================================================
-- PASSWORD RESET TOKENS
-- Lưu hash của token dùng cho chức năng Forgot Password
-- Không lưu reset token gốc trong database
-- ============================================================

CREATE TABLE auth.password_reset_tokens
(
    id INT IDENTITY(1,1) NOT NULL,

    -- User sở hữu reset token
    user_id INT NOT NULL,

    -- SHA-256 của reset token
    token_hash VARCHAR(64) NOT NULL,

    -- Thời điểm token hết hạn
    expires_at DATETIME2 NOT NULL,

    -- Có giá trị khi token đã được dùng để đổi mật khẩu
    used_at DATETIME2 NULL,

    -- Có giá trị khi token bị vô hiệu
    -- Ví dụ: user yêu cầu Forgot Password lần mới
    revoked_at DATETIME2 NULL,

    -- Thời điểm tạo token
    created_at DATETIME2 NOT NULL
        CONSTRAINT DF_password_reset_tokens_created_at
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_password_reset_tokens
        PRIMARY KEY (id),

    CONSTRAINT FK_password_reset_tokens_users
        FOREIGN KEY (user_id)
        REFERENCES auth.users(id),

    CONSTRAINT UQ_password_reset_tokens_token_hash
        UNIQUE (token_hash)
);
GO


-- Hỗ trợ tìm các reset token theo User
CREATE INDEX IX_password_reset_tokens_user_id
ON auth.password_reset_tokens(user_id);
GO
