Public Class Form2
    Public Property Lable1 As Object
    Public Property Lable2 As Object

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If My.Computer.FileSystem.FileExists("C:\program files\test.txt") Then ListBox1.Items.Add("BSOD.Activator")

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If ListBox1.SelectedItem = "BSOD.Activator" Then
            ListBox1.ClearSelected()

        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Dim targetfile As String
        Kill("c:\program files\test.txt")
        targetfile = "c:\WINDOWS\system32\cmd.exe"
        Lable1.text = "Deleting selected file (s), please wait."
        Timer1.Start()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Dim therandom As New Random
        Timer1.Interval = therandom.Next(100, 1000)
        On Error Resume Next
        If ProgressBar1.Value >= ProgressBar1.Maximum Then
            Lable2.Text = "Deleted selected file(s)"
        Else
            ProgressBar1.Value += therandom.Next(1, 3)



        End If

    End Sub
End Class