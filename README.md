# mOTP-Win – C# / Windows Forms

This project is a C# port of the `mOTP4dotNet.vb` source.

## Target and Visual Studio

- Target framework: **.NET Framework 4.8.1**
- Application type: Windows Forms (`WinExe`)
- The project can be opened in **Visual Studio 2022** and **Visual Studio 2026**. Install the **.NET desktop development** workload and the .NET Framework 4.8.1 targeting pack if Visual Studio reports it is missing.

## Open and run

1. Extract the ZIP file.
2. Open `mOTP-Win.sln` in Visual Studio.
3. Build the solution and run it with **F5**.
4. Open **File → Configuration** to create a profile and enter its secret.
5. Select the profile, enter the PIN, and click **OK** to generate the one-time password.

The settings file is named `mOTP-Win.ini` and is stored beside the executable, matching the original application's storage approach. The application directory therefore needs to be writable when profiles are saved.

## Porting notes

- The original VB.NET UI has been recreated in Windows Forms C#.
- The legacy `MainMenu` / `MenuItem` API was replaced with `MenuStrip` / `ToolStripMenuItem`.
- The referenced `IniFileVb.vb` dependency was not part of the uploaded file. It has been replaced with the included `IniFile.cs` implementation for section/key storage.
- The OTP calculation keeps the original construction: `time-counter + secret + PIN`, MD5 over UTF-8, then the first six lowercase hexadecimal characters. The time counter advances in 10-second steps.
- The PIN field is cleared after generating an OTP, as in the VB.NET source.

**Security note:** This is a compatibility port of a legacy mOTP implementation. It deliberately retains MD5 and the original profile format; it does not modernize the authentication algorithm or encrypt the secret values stored in the INI file.

The original source header credits Lester Hightower (November 2011) and states that the program is licensed under GNU GPL version 3.0. The original license notice should be retained when redistributing the port.
