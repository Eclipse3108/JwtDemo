using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace jwt_demoV2.Controllers;

[ApiController]
[Route("[controller]")]
public class ChuDeController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly JwtToken _jwt;

    public ChuDeController(IConfiguration configuration)
    {
        _configuration = configuration;
        _jwt = new JwtToken();
    }

    // Phương thức GET này lấy tất cả danh sách chủ đề hoặc lấy chủ đề theo mã được truyền vào là chuDeId
    // int? là cách khai báo có thể truyền vào mã chủ đề hoặc có thể không truyền
    // FromQuery: Khi gọi api có dạng
    //      1. http://localhost/chude               lấy tất cả danh sách chủ đề
    //      2. http://localhost/chude?chuDeId=1     lấy chủ đề theo mã chủ đề cụ thể do người dùng truyền vào
    [HttpGet(Name = "Get_Chu_De")]
    public async Task<ActionResult> GetChuDe([FromQuery] int? chuDeId)
    {
        try
        {
            string authHeader = "";
            string token = "";

            // Lấy token từ header Authorization
            authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Missing or invalid Authorization header" });
            }

            token = authHeader["Bearer ".Length..].Trim();

            // Gọi hàm ValidateToken
            if (_jwt.ValidateToken(token, out ClaimsPrincipal? claims))
            {
                // Token hợp lệ và lấy thông tin userName trong payload
                var userName = claims?.FindFirst(c => c.Type == "UserName")?.Value;

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using SqlConnection connection = new(connectionString);
                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                using SqlCommand command = new();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;

                // Nếu người dùng có truyền mã chủ đề
                if (chuDeId.HasValue)
                {
                    command.CommandText = "spDanhMuc_ChuDeSelectById";
                    command.Parameters.AddWithValue("@ChuDeID", chuDeId);
                }
                else
                {
                    command.CommandText = "spDanhMuc_ChuDeSelectALL";
                }

                SqlDataAdapter da = new(command);
                DataTable dt = new();
                da.Fill(dt);

                // Server trả dữ liệu về cho client theo định dạng JSON
                return new ContentResult
                {
                    Content = JsonConvert.SerializeObject(new { data = dt }),
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            else
            {
                // Token không hợp lệ
                return Unauthorized(new { message = "Token is invalid" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message.ToString() });
        }
    }

    // Phương thức GET này lấy tất cả danh sách chủ đề hoặc lấy chủ đề theo mã được truyền vào
    // int? là cách khai báo có thể truyền vào mã chủ đề hoặc có thể không truyền
    // Cách thức khi gọi api trong trường hợp này như sau
    //      1. http://localhost/chude       lấy tất cả danh sách chủ đề
    //      2. http://localhost/chude/1     lấy chủ đề theo mã chủ đề cụ thể
    [HttpGet("{chuDeId?}", Name = "Get_Chu_De_With_Another_Way")]
    public async Task<ActionResult> GetChuDeOther(int? chuDeId)
    {
        try
        {
            string authHeader = "";
            string token = "";

            // Lấy token từ header Authorization
            authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Missing or invalid Authorization header" });
            }

            token = authHeader["Bearer ".Length..].Trim();

            // Gọi hàm ValidateToken
            if (_jwt.ValidateToken(token, out ClaimsPrincipal? claims))
            {
                // Token hợp lệ và lấy thông tin userName trong payload
                var userName = claims?.FindFirst(c => c.Type == "UserName")?.Value;

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using SqlConnection connection = new(connectionString);
                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                using SqlCommand command = new();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;

                // Nếu có truyền vào mã chủ đề
                if (chuDeId.HasValue)
                {
                    command.CommandText = "spDanhMuc_ChuDeSelectById";
                    command.Parameters.AddWithValue("@ChuDeID", chuDeId);
                }
                else
                {
                    command.CommandText = "spDanhMuc_ChuDeSelectALL";
                }

                SqlDataAdapter da = new(command);
                DataTable dt = new();
                da.Fill(dt);

                // Server trả dữ liệu về cho client theo định dạng JSON
                return new ContentResult
                {
                    Content = JsonConvert.SerializeObject(new { data = dt }),
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            else
            {
                // Token không hợp lệ
                return Unauthorized(new { message = "Token is invalid" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message.ToString() });
        }
    }

    // api chude/update dùng để thêm hoặc chỉnh sửa thông tin chủ đề
    // các tham số (mã chủ đề, tên chủ đề) được truyền trong phần Body
    // trong câu thủ tục có kiểm tra quyền của người dùng được phép thêm hoặc sửa dữ liệu hay không
    // thông tin người dùng được lấy từ payload trong token được client truyền vào phần header của gói tin
    [HttpPost("update")]
    public async Task<ActionResult> UpdateChuDe()
    {
        try
        {
            string authHeader = "";
            string token = "";

            // Lấy token từ header Authorization
            authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Missing or invalid Authorization header" });
            }

            token = authHeader["Bearer ".Length..].Trim();

            // Gọi hàm ValidateToken
            if (_jwt.ValidateToken(token, out ClaimsPrincipal? claims))
            {
                // Token hợp lệ và lấy thông tin userName trong payload
                var userName = claims?.FindFirst(c => c.Type == "UserName")?.Value;

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using SqlConnection connection = new(connectionString);
                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                using SqlCommand command = new();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "spDanhMuc_ChuDeUpdate";

                // Lấy thông tin MaCD và TenChuDe trong Body
                using (var reader = new StreamReader(Request.Body))
                {
                    var requestBody = await reader.ReadToEndAsync();
                    var jsonObject = JObject.Parse(requestBody);

                    // Thêm các tham số cho thủ tục
                    command.Parameters.AddWithValue("@MaCD", jsonObject["MaCD"]?.Value<int>());
                    command.Parameters.AddWithValue("@TenChuDe", jsonObject["TenChuDe"]?.Value<string>());
                    command.Parameters.AddWithValue("@UserName", userName);
                }

                SqlDataAdapter da = new(command);
                DataTable dt = new();
                da.Fill(dt);

                // Server gửi thông báo về cho client
                // dt.Rows[0]["errMsg"] đây là dữ liệu mà trong câu thủ tục trả về               
                return new ContentResult
                {
                    Content = JsonConvert.SerializeObject(new { message = dt.Rows[0]["errMsg"] }),
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            else
            {
                // Token không hợp lệ
                return Unauthorized(new { message = "Token is invalid" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message.ToString() });
        }
    }

    // api chude/delete dùng để xoá thông tin chủ đề
    // tham số mã chủ đề được truyền trong phần Body
    // trong câu thủ tục có kiểm tra quyền của người dùng được phép xoá dữ liệu hay không
    // thông tin người dùng được lấy từ payload trong token được client truyền vào phần header của gói tin
    [HttpPost("delete")]
    public async Task<ActionResult> DeleteChuDe()
    {
        try
        {
            string authHeader = "";
            string token = "";

            // Lấy token từ header Authorization
            authHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Missing or invalid Authorization header" });
            }

            token = authHeader["Bearer ".Length..].Trim();

            // Gọi hàm ValidateToken
            if (_jwt.ValidateToken(token, out ClaimsPrincipal? claims))
            {
                // Token hợp lệ và lấy thông tin userName trong payload
                var userName = claims?.FindFirst(c => c.Type == "UserName")?.Value;

                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using SqlConnection connection = new(connectionString);
                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                using SqlCommand command = new();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "spDanhMuc_ChuDeDelete";

                // Lấy thông tin MaCD và TenChuDe trong Body
                using (var reader = new StreamReader(Request.Body))
                {
                    var requestBody = await reader.ReadToEndAsync();
                    var jsonObject = JObject.Parse(requestBody);

                    // Thêm các tham số cho thủ tục
                    command.Parameters.AddWithValue("@MaCD", jsonObject["MaCD"]?.Value<int>());
                    command.Parameters.AddWithValue("@UserName", userName);
                }

                SqlDataAdapter da = new(command);
                DataTable dt = new();
                da.Fill(dt);

                // Server gửi thông báo về cho client
                // dt.Rows[0]["errMsg"] đây là dữ liệu mà trong câu thủ tục trả về
                return new ContentResult
                {
                    Content = JsonConvert.SerializeObject(new { message = dt.Rows[0]["errMsg"] }),
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            else
            {
                // Token không hợp lệ
                return Unauthorized(new { message = "Token is invalid" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message.ToString() });
        }
    }
}
