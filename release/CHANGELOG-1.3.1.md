## Fluent Prayer Times 1.3.1

### Fixed
- Installing or updating while the app is running in the system tray no longer fails with "Setup was unable to automatically close all applications". Both installers now close the running app automatically, wait for it to exit, and start it again in the tray after an update.
- Uninstalling while the app is running closes it automatically first.

### Downloads / التنزيلات
- Portable ZIP: extract and run. Requires .NET 8 x64, Visual C++ x64 and Windows App Runtime 1.6.
- Self-contained installer: includes its own .NET runtime so setup can start without a preinstalled runtime.
- Small .NET-runtime-dependent installer: native setup checks and installs missing requirements from Microsoft.
- Both installers add Start menu and desktop shortcuts. Missing dependencies require an internet connection and administrator approval.
- Windows 10 1809+ / Windows 11, x64. Unsigned application: Windows SmartScreen may show a warning.

## سجل التغييرات - الإصدار 1.3.1

### الإصلاحات
- لم يعد التثبيت أو التحديث أثناء تشغيل البرنامج في شريط النظام يفشل برسالة "Setup was unable to automatically close all applications". يغلق المثبتان الآن البرنامج تلقائيًا وينتظران خروجه، ثم يعيدان تشغيله في شريط النظام بعد التحديث.
- إلغاء التثبيت أثناء تشغيل البرنامج يغلقه تلقائيًا أولًا.

### التنزيلات
- النسخة المحمولة: فك الضغط وشغّل البرنامج. تتطلب .NET 8 وVisual C++ وWindows App Runtime 1.6 بنظام x64.
- المثبت الكبير: يتضمن بيئة تشغيله الخاصة لبدء التثبيت دون تثبيت .NET مسبقًا.
- المثبت الصغير المعتمد على .NET: يفحص المتطلبات ويثبّت الناقص من Microsoft بواجهة تثبيت أصلية.
- يضيف المثبتان اختصارات إلى قائمة ابدأ وسطح المكتب. يلزم اتصال بالإنترنت للمتطلبات الناقصة وصلاحيات المسؤول.
- يدعم Windows 10 1809 والإصدارات الأحدث وWindows 11 بنظام x64. البرنامج غير موقّع، وقد يعرض Windows SmartScreen تحذيرًا.
