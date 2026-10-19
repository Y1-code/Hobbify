using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    /// <summary>
    /// כתובות ה-API של המשתמשים. כל הכתובות מתחילות ב-/api/users.
    /// כל פעולה מפעילה שאילתה אחת מ-UserDB ומחזירה קוד מצב מתאים.
    ///
    /// שים לב: בשלב הזה הכתובות פתוחות לכולם. בדיקת הרשאות לפי תפקיד
    /// (למשל שרק SuperAdmin יכול לשנות תפקיד) תתווסף בשלב ההתחברות.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDB userDb;

        // UserDB מגיע אוטומטית (Dependency Injection), כי רשמנו אותו ב-Program.cs.
        public UsersController(UserDB userDb)
        {
            this.userDb = userDb;
        }

        // GET /api/users
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            return Ok(await userDb.GetAllUsersAsync());
        }

        // GET /api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            User? user = await userDb.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound($"משתמש {id} לא נמצא");
            }
            return Ok(user);
        }

        // GET /api/users/email-exists?email=dana@example.com
        [HttpGet("email-exists")]
        public async Task<ActionResult<bool>> EmailExists([FromQuery] string email)
        {
            return Ok(await userDb.EmailExistsAsync(email));
        }

        // GET /api/users/count-by-role
        [HttpGet("count-by-role")]
        public async Task<ActionResult<Dictionary<string, int>>> CountByRole()
        {
            return Ok(await userDb.CountUsersByRoleAsync());
        }

        // POST /api/users/register
        // הרשמה של הורה חדש. התפקיד תמיד Parent: אי אפשר להירשם כמנהל דרך הכתובת הזו.
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterRequest request)
        {
            if (await userDb.EmailExistsAsync(request.Email))
            {
                return Conflict("האימייל כבר רשום במערכת");
            }

            var user = new User(request.FullName, request.Email, request.Phone, Roles.Parent);
            int newId = await userDb.AddUserAsync(user, request.Password);

            User? created = await userDb.GetUserByIdAsync(newId);
            return CreatedAtAction(nameof(GetById), new { id = newId }, created);
        }

        // POST /api/users/login
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginRequest request)
        {
            User? user = await userDb.CheckLoginAsync(request.Email, request.Password);
            if (user == null)
            {
                // אותה הודעה לאימייל שגוי, לסיסמה שגויה ולמשתמש חסום,
                // כדי לא לחשוף אילו אימיילים רשומים.
                return Unauthorized("אימייל או סיסמה שגויים");
            }
            return Ok(user);
        }

        // PUT /api/users/5/role?role=Manager
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string role)
        {
            if (!Roles.IsValid(role))
            {
                return BadRequest($"תפקיד לא קיים. אפשרויות: {string.Join(", ", Roles.All)}");
            }

            bool updated = await userDb.UpdateRoleAsync(id, role);
            return updated ? NoContent() : NotFound($"משתמש {id} לא נמצא");
        }

        // PUT /api/users/5/password   (גוף הבקשה: "NewPass123")
        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return BadRequest("הסיסמה צריכה להכיל לפחות 6 תווים");
            }

            bool updated = await userDb.UpdatePasswordAsync(id, newPassword);
            return updated ? NoContent() : NotFound($"משתמש {id} לא נמצא");
        }

        // PUT /api/users/5/email   (גוף הבקשה: "new@example.com")
        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return BadRequest("כתובת האימייל לא תקינה");
            }

            User? current = await userDb.GetUserByIdAsync(id);
            if (current == null)
            {
                return NotFound($"משתמש {id} לא נמצא");
            }

            // תפוס על ידי משתמש אחר? (אותו אימייל של אותו משתמש זה בסדר)
            if (!string.Equals(current.Email, email, StringComparison.OrdinalIgnoreCase)
                && await userDb.EmailExistsAsync(email))
            {
                return Conflict("האימייל כבר רשום במערכת");
            }

            await userDb.UpdateEmailAsync(id, email);
            return NoContent();
        }

        // PUT /api/users/5/details   (גוף הבקשה: { "fullName": "...", "phone": "..." })
        [HttpPut("{id:int}/details")]
        public async Task<IActionResult> UpdateDetails(int id, [FromBody] UpdateDetailsRequest request)
        {
            bool updated = await userDb.UpdateDetailsAsync(id, request.FullName, request.Phone);
            return updated ? NoContent() : NotFound($"משתמש {id} לא נמצא");
        }

        // PUT /api/users/5/has-updates?value=false
        // הפעמון: false כשההורה לוחץ על הפעמון, true כשמנהל מתנס מטפל בבקשה שלו.
        [HttpPut("{id:int}/has-updates")]
        public async Task<IActionResult> SetHasUpdates(int id, [FromQuery] bool value)
        {
            bool updated = await userDb.SetHasUpdatesAsync(id, value);
            return updated ? NoContent() : NotFound($"משתמש {id} לא נמצא");
        }

        // PUT /api/users/5/active?value=false
        // חסימת משתמש (false) או החזרה לפעילות (true). משתמש חסום לא יכול להתחבר.
        [HttpPut("{id:int}/active")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool value)
        {
            bool updated = await userDb.SetActiveAsync(id, value);
            return updated ? NoContent() : NotFound($"משתמש {id} לא נמצא");
        }

        // DELETE /api/users/5
        // לניקוי משתמשי בדיקה בלבד. במערכת עצמה לא מוחקים משתמשים, אלא חוסמים.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await userDb.GetUserByIdAsync(id) == null)
            {
                return NotFound($"משתמש {id} לא נמצא");
            }

            bool deleted = await userDb.DeleteUserAsync(id);
            return deleted
                ? NoContent()
                : Conflict("אי אפשר למחוק משתמש שיש לו הרשמות או מתנס");
        }
    }
}