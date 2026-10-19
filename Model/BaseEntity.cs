namespace Model
{
    /// <summary>
    /// מחלקת הבסיס של כל הישויות בפרויקט (משתמש, עיר, מתנס, חוג...).
    /// לכל טבלה יש מפתח ראשי מספרי, ולכן המזהה מוגדר כאן פעם אחת.
    /// abstract: אי אפשר ליצור "ישות" סתם, רק ישות מסוג מסוים.
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        protected BaseEntity()
        {
        }

        protected BaseEntity(int id)
        {
            Id = id;
        }
    }
}