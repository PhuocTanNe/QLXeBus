using System;
using System.Collections.Generic;
using QLXeBus.Models;

namespace QLXeBus.Services
{
    /// <summary>Người dùng đang đăng nhập và các quyền (mã chức năng) của nhóm.</summary>
    public static class Session
    {
        static HashSet<string> _perm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static CurrentUser User { get; private set; }
        public static int LoginLogId { get; private set; }

        public static void Start(CurrentUser u, IEnumerable<string> permissions, int loginLogId)
        {
            User = u;
            LoginLogId = loginLogId;
            _perm = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
        }

        public static void Clear() { User = null; _perm.Clear(); LoginLogId = 0; }

        public static bool Can(string maChucNang) { return _perm.Contains(maChucNang); }

        public static void Require(string maChucNang)
        {
            if (!Can(maChucNang))
                throw new UnauthorizedAccessException("Bạn không có quyền thực hiện chức năng này (" + maChucNang + ").");
        }
    }
}
