# RTL Terminal for Windows

טרמינל Windows עצמאי עם תמיכה מלאה ב-RTL (עברית, ערבית וכו').

## דרישות

- Windows 10 ומעלה
- .NET 6.0 SDK (או גרסה חדשה יותר)
- Visual Studio 2022, VS Code, או JetBrains Rider (optional)

## בנייה וריצה

### דרך 1: Command Line (הדרך הפשוטה)

```bash
# עברו לתיקיה המכילה את הקבצים
cd C:\path\to\RTLTerminal

# קמפלו את הפרויקט
dotnet build

# הריצו את האפליקציה
dotnet run
```

### דרך 2: Visual Studio

1. פתחו את `RTLTerminal.csproj` ב-Visual Studio 2022
2. לחצו `Build -> Build Solution` (Ctrl+Shift+B)
3. לחצו `Debug -> Start Debugging` (F5)

## איך זה עובד

### ארכיטקטורה
- **Process Management**: מריץ `cmd.exe` דרך Windows Process API
- **Input/Output**: משלח קמנדים דרך stdin, קורא פלט דרך stdout/stderr
- **RTL Rendering**: WPF מטפל אוטומטית בـ Unicode Bidi Algorithm

### פיצ'רים

✅ טרמינל עצמאי לא כרך עם cmd  
✅ הרצת כל קמנד cmd.exe רגיל  
✅ תמיכה מלאה RTL (עברית, ערבית, וכו')  
✅ תמיכה mixed text (עברית + אנגלית בשורה אחת)  
✅ צבעים שונים (output לבן, errors אדום, input cyan)  
✅ auto-scroll to bottom  

## דוגמאות שימוש

```
> dir
> cd C:\Users
> type file.txt
> echo שלום עולם
> python script.py
```

## פתרון בעיות

### לא קמפיל?
- ודאו ש-.NET 6.0 SDK מותקן: `dotnet --version`
- נסו: `dotnet restore` ואחרי כך `dotnet build`

### RTL לא מופיע נכון?
- בקו של `AppendOutput`, אנחנו משתמשים ב-`Paragraph` של WPF
- WPF עצמו מטפל בـ BiDi - אם טקסט עדיין לא נראה נכון, בדקו את קידוד UTF-8:
  ```csharp
  _cmdProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
  ```

## עידכונים אפשריים

- **ANSI Color Support**: הוסיפו פרסור למימוש צבעים של ANSI escape sequences
- **Font Selection**: הוסיפו menu לשינוי font וגודל
- **Tabs**: הוסיפו support למספר tabs/windows
- **Mouse Support**: הוסיפו mouse wheel scroll ו-right-click menu

## ליסנס

קוד זה חופשי לשימוש והערכה.
