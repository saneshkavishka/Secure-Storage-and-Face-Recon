Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Public Class Form1
    Dim increment As Integer = 0

    Public Function Rijndael_Encrypt(bytesForEncrypted As Byte(), passwordBytes As Byte()) As Byte()

        Dim Rythorian77 As String = TextBox1.Text

        Dim pass As String = "123456789ABCDEFG!@#$%^&*()_+"

        Dim key As Byte() = New Byte(31) {}

        'When overridden in a derived class, encodes a set of characters into a sequence of bytes.
        Encoding.Default.GetBytes(pass).CopyTo(key, 0)

        ' Encoding.Default.GetBytes(pass).CopyTo(saltBytes, 0)
        Using sphere As New MemoryStream

            Using AES As New RijndaelManaged()
                AES.KeySize = 256
                AES.BlockSize = 256
                Dim kay = New Rfc2898DeriveBytes(passwordBytes, key, 1000)
                AES.Key = kay.GetBytes(AES.KeySize / 8)
                AES.IV = kay.GetBytes(AES.BlockSize / 8)
                AES.Mode = CipherMode.CBC

                Using cs = New CryptoStream(sphere, AES.CreateEncryptor(), CryptoStreamMode.Write)
                    cs.Write(bytesForEncrypted, 0, bytesForEncrypted.Length)
                    cs.Close()
                End Using

                key = sphere.ToArray()
            End Using
        End Using

        Return key
    End Function


    Public Function Rijndael_Decrypt(bytesForDecrypted As Byte(), passwordBytes As Byte()) As Byte()

        Dim Rythorian77 As String = TextBox1.Text

        Dim pass As String = "123456789ABCDEFG!@#$%^&*()_+"

        Dim key As Byte() = New Byte(31) {}

        'When overridden in a derived class, encodes a set of characters into a sequence of bytes.
        Encoding.Default.GetBytes(pass).CopyTo(key, 0)

        ' Encoding.Default.GetBytes(pass).CopyTo(saltBytes, 0)
        Using sphere As New MemoryStream

            Using AES As New RijndaelManaged()
                AES.KeySize = 256
                AES.BlockSize = 256
                Dim kay = New Rfc2898DeriveBytes(passwordBytes, key, 1000)
                AES.Key = kay.GetBytes(AES.KeySize / 8)
                AES.IV = kay.GetBytes(AES.BlockSize / 8)
                AES.Mode = CipherMode.CBC

                Using cs = New CryptoStream(sphere, AES.CreateDecryptor(), CryptoStreamMode.Write)
                    cs.Write(bytesForDecrypted, 0, bytesForDecrypted.Length)
                    cs.Close()
                End Using

                key = sphere.ToArray()
            End Using
        End Using

        Return key
    End Function

    'encrypt 
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ProgressBar1.Visible = True
        increment += 100
        If increment > ProgressBar1.Maximum Then
            increment = ProgressBar1.Maximum
        End If
        ProgressBar1.Value = increment

        Dim file As String = TextBox1.Text

        Dim pass As String = ""

        Try
            Dim bytesToBeEncrypted As Byte() = IO.File.ReadAllBytes(file)

            Dim passwordBytes As Byte() = Encoding.UTF8.GetBytes(pass)

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes)

            Dim bytesEncrypted As Byte() = Rijndael_Encrypt(bytesToBeEncrypted, passwordBytes)

            IO.File.WriteAllBytes(file, bytesEncrypted)

            IO.File.Move(file, file & ".Rythorian")

        Catch ex As Exception
            MsgBox(ex.ToString())
        End Try


    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        ProgressBar2.Visible = True
        increment += 100
        If increment > ProgressBar2.Maximum Then
            increment = ProgressBar2.Maximum
        End If
        ProgressBar2.Value = increment

        Dim file As String = TextBox1.Text

        Dim pass As String = ""
        Try
            Dim bytesToBeDecrypted As Byte() = IO.File.ReadAllBytes(file)

            Dim passwordBytes As Byte() = Encoding.UTF8.GetBytes(pass)

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes)

            Dim bytesDecrypted As Byte() = Rijndael_Decrypt(bytesToBeDecrypted, passwordBytes)

            IO.File.WriteAllBytes(file, bytesDecrypted)

            Dim extension As String = Path.GetExtension(file)

            Dim result As String = file.Substring(0, file.Length - extension.Length)

            IO.File.Move(file, result)

        Catch ex As Exception
            MsgBox(ex.ToString())
        End Try
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Allows you to access files from folders
        Dim CryptDialog As New OpenFileDialog With {
            .CheckFileExists = True,
            .InitialDirectory = "C:\",
            .Multiselect = False
        }

        If CryptDialog.ShowDialog = DialogResult.OK Then
            TextBox1.Text = CryptDialog.FileName
        End If
    End Sub
End Class
