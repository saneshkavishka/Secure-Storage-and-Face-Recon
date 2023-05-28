'Imports MySql.Data.MySqlClient
Imports System.Data.SQLite


#Region "View face data"


Public Class temp

    Dim sql As String

    Dim cmd As SQLiteCommand
    Dim dr As SQLiteDataReader
    Dim mysqlconn As SQLiteConnection

    Private Sub temp_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load

    End Sub


    Sub loadme2()

        Dim checkstate As String = ""
        Dim arrImage() As Byte = Nothing
        Dim arrImage2() As Byte = Nothing

        ListView1.Items.Clear()
        mysqlconn = New SQLiteConnection
        mysqlconn.ConnectionString = Form1.connection
        mysqlconn.Open()
        Dim sqlstr As String = "SELECT id,ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y FROM Faceinfo"
        Dim cmd = New SQLiteCommand(sqlstr, mysqlconn)
        Dim sqlReader As SQLiteDataReader = cmd.ExecuteReader
        For i As Integer = 1 To sqlReader.FieldCount
            Dim li As ListViewItem
            ListView1.BeginUpdate()
            While sqlReader.Read
                li = ListView1.Items.Add(sqlReader("id"))
                li.SubItems.Add(sqlReader("ImageFileName").ToString.ToUpper())

                li.SubItems.Add(sqlReader("FacePositionXc"))
                li.SubItems.Add(sqlReader("FacePositionYc"))
                li.SubItems.Add(sqlReader("FacePositionW"))
                li.SubItems.Add(sqlReader("FacePositionAngle"))

                li.SubItems.Add(sqlReader("Eye1X"))
                li.SubItems.Add(sqlReader("Eye1Y"))

                li.SubItems.Add(sqlReader("Eye2X"))
                li.SubItems.Add(sqlReader("Eye2Y"))


            End While
            ListView1.EndUpdate()
        Next

        mysqlconn.Close()
        cmd.Dispose()
        sqlReader.Dispose()

        For i As Integer = 0 To ListView1.Items.Count - 1
            If i Mod 2 Then
                ListView1.Items(i).BackColor = Color.LightBlue
            Else
                ListView1.Items(i).BackColor = Color.White
            End If
        Next
    End Sub

    Private Sub temp_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown
        loadme2()
    End Sub

    Sub selectpic()
        Dim cmd As New SQLiteCommand
        Dim dr As SQLiteDataReader
        Dim sup As Integer
        Dim arrImage() As Byte = Nothing
        Dim arrImage2() As Byte = Nothing

        mysqlconn = New SQLiteConnection
        mysqlconn.ConnectionString = Form1.connection

        'check if asset alreadyy exist
        sql = "SELECT Image, FaceImage FROM faceinfo WHERE id='" & ListView1.FocusedItem.SubItems(0).Text & "'"
        If mysqlconn.State = ConnectionState.Closed Then mysqlconn.Open()
        cmd = New SQLiteCommand(sql, mysqlconn)
        dr = cmd.ExecuteReader()
        While (dr.Read())
            arrImage = (dr("Image"))
            arrImage2 = (dr("FaceImage"))
        End While
        dr.Close()

        Dim mstream As New System.IO.MemoryStream(arrImage)
        Dim mstreamm As New System.IO.MemoryStream(arrImage2)

        Dim img As Image = Image.FromStream(mstream)
        Dim img_face As Image = Image.FromStream(mstreamm)

        PictureBox1.Image = img_face
        Label1.Text = ListView1.FocusedItem.SubItems(1).Text
    End Sub

    Private Sub ListView1_Click(sender As Object, e As System.EventArgs) Handles ListView1.Click
        selectpic()
    End Sub

    Private Sub ListView1_MouseUp(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles ListView1.MouseUp
        If (e.Button <> MouseButtons.Left) Then
            Dim itemAt As ListViewItem = ListView1.GetItemAt(e.X, e.Y)
            If ListView1.GetItemAt(e.X, e.Y) IsNot Nothing Then
                ListView1.GetItemAt(e.X, e.Y).Selected = True
                ContextMenuStrip1.Show(ListView1, New Point(e.X, e.Y))
            End If
        End If
    End Sub

    Private Sub DeleteToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles DeleteToolStripMenuItem.Click
        Dim sql As String
        mysqlconn = New SQLiteConnection
        mysqlconn.ConnectionString = Form1.connection

        ListView1.Cursor = Cursors.WaitCursor
        Try
            Dim cmd As New SQLiteCommand
            If mysqlconn.State = ConnectionState.Closed Then mysqlconn.Open()
            sql = "delete from faceinfo where id ='" & ListView1.FocusedItem.SubItems(0).Text & "'"

            cmd = New SQLiteCommand(sql, mysqlconn)
            cmd.ExecuteNonQuery()

        Catch ex As SQLiteException
            MsgBox("An Error Occurred. " & ex.ErrorCode & " – " & ex.Message)
        End Try



        loadme2()
        Form1.loadme()

        MsgBox("Operation successfully Executed")
        ListView1.Cursor = Cursors.Default
    End Sub

End Class
#End Region