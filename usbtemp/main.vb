Imports System.IO
Imports System.Xml.Serialization

Public Class Main

    'Public Device As USBDevice
    Public RedLab_8Out As USBDevice
    Public RedLab_16In As USBDevice
    Public USBTemp As USBDevice

    Private WithEvents InstallationProcess As Process

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Initialize()

        If Not IO.File.Exists("C:\Program Files (x86)\Measurement Computing\DAQ\inscal32.exe") Then
            Dim downloadpath As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\Downloads\"
            IO.File.WriteAllBytes(downloadpath & "icalsetup.exe", My.Resources.icalsetup)
            If IO.File.Exists(downloadpath & "icalsetup.exe") Then _
            Process.Start(downloadpath & "icalsetup.exe")
        Else
            'If Device.Ready Then
            '    Text = Device.Name
            'Else
            '    Text = "No USB-Device connected!"
            'End If
        End If
    End Sub

    Private Sub Initialize()
        'Device = New USBDevice(0) 'Index 0 für erstes Board
        RedLab_8Out = New USBDevice(1, USBDevice.USBDevice.RedLab3103)
        ObjectListView1.AddObjects(RedLab_8Out.OutputChannel)

        RedLab_16In = New USBDevice(0, USBDevice.USBDevice.RedLab2416_4AO)
        ObjectListView1.AddObjects(RedLab_16In.InputChannel)

        'usbtemp = New USBDevice(0, USBDevice.USBDevice.RedLab2416_4AO)

        ZedGraphControl1.GraphPane.XAxis.Type = ZedGraph.AxisType.Date
        ZedGraphControl1.GraphPane.XAxis.Title.Text = "Date"
        ZedGraphControl1.GraphPane.YAxis.Title.Text = "[-]"
        ZedGraphControl1.GraphPane.Title.Text = "Plotter"
    End Sub

    Private Function GetColor(index As Integer) As Color
        Select Case index
            Case 0
                Return Color.Black
            Case 1
                Return Color.Red
            Case 2
                Return Color.Blue
            Case 3
                Return Color.Green
            Case 4
                Return Color.Brown
            Case 5
                Return Color.Pink
            Case 6
                Return Color.Orange
            Case 7
                Return Color.LightBlue
            Case 8
                Return Color.LightGreen
        End Select
    End Function

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        Timer1.Start()
        ToolStripButton1.Enabled = False
        ToolStripButton2.Enabled = True
    End Sub

    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        Timer1.Stop()
        ToolStripButton1.Enabled = True
        ToolStripButton2.Enabled = False
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        RedLab_16In.MeasureAll()
        'USBTemp.MeasureAll()

        For Each c As Channel In ObjectListView1.SelectedObjects
            Dim line As ZedGraph.CurveItem = ZedGraphControl1.GraphPane.CurveList.Item(c.Name)
            If line IsNot Nothing Then line.AddPoint(c.data.Last.MeasurementDate.ToOADate, c.data.Last.Value)
        Next
        ZedGraphControl1.AxisChange()
        ZedGraphControl1.Refresh()
    End Sub

    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
        If MsgBox("Sicher alles löschen?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            Timer1.Stop()
            'Device.Clear()
            'For i As Integer = 0 To 7
            '    ZedGraphControl1.GraphPane.CurveList(i).Clear()
            '    Dim cb As CheckBox = Controls.Find("CheckBox" & i + 1, True).First
            '    cb.Text = "Channel " & i + 1
            'Next i
            'ZedGraphControl1.Refresh()
        End If
    End Sub

    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        Using sfd As New SaveFileDialog
            If sfd.ShowDialog = DialogResult.OK Then
                RedLab_16In.Export(sfd.FileName)
                RedLab_8Out.Export(sfd.FileName)
            End If
        End Using
    End Sub

    Private Sub ToolStripButton5_Click(sender As Object, e As EventArgs) Handles ToolStripButton5.Click
        Process.Start("C:\Program Files (x86)\Measurement Computing\DAQ\inscal32.exe")
    End Sub

    Private Sub ToolStripComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox1.SelectedIndexChanged
        Dim value As Integer = ToolStripComboBox1.Text
        Timer1.Interval = value * 1000
    End Sub

    Private Sub ToolStripButton6_Click(sender As Object, e As EventArgs) Handles ToolStripButton6.Click
        AboutBox.ShowDialog(Me)
    End Sub

    Private Sub Main_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If MsgBox("Sure u want to close and stop measurement?", vbYesNo) = MsgBoxResult.Yes Then
            e.Cancel = False
        Else
            e.Cancel = True
        End If
    End Sub

    Private Sub ObjectListView1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ObjectListView1.SelectedIndexChanged
        ZedGraphControl1.GraphPane.CurveList.Clear()
        Dim i As Integer = 0
        For Each c As Channel In ObjectListView1.SelectedObjects
            ZedGraphControl1.GraphPane.CurveList.Add(New ZedGraph.LineItem(c.Name) With
                {.Color = GetColor(i), .Symbol = New ZedGraph.Symbol(ZedGraph.SymbolType.None, GetColor(i))})
            i += 1
        Next
    End Sub

End Class

Public Class USBDevice
    Private Index As Integer
    Private DaqBoard As MccDaq.MccBoard

    Public InputChannel() As Channel
    Public OutputChannel() As Channel

    Public Ready As Boolean = True
    Public DeviceType As USBDevice

    Public Resolution As Integer
    Private WandlerMax As Integer

    Public ReadOnly Property Name() As String
        Get
            Return DaqBoard.BoardName & " :ID=" & Index
        End Get
    End Property

    Public Enum USBDevice
        RedLab3103
        RedLab2416_4AO
        MeasurementComputing_USBTemp
    End Enum

    Public Sub New()
    End Sub

    Public Sub New(Index As Integer, DeviceType As USBDevice)
        Me.Index = Index
        Me.DeviceType = DeviceType

        DaqBoard = New MccDaq.MccBoard(Index)
        Dim cnf As MccDaq.cBoardConfig = DaqBoard.BoardConfig

        Dim Count As Integer
        Select Case DeviceType
            Case USBDevice.MeasurementComputing_USBTemp
                cnf.GetNumTempChans(Count)
                ReDim InputChannel(Count - 1)
                For i As Integer = 0 To InputChannel.Count - 1
                    InputChannel(i) = New Channel("USBTemp Temp_" & i + 1)
                Next
            Case USBDevice.RedLab2416_4AO
                cnf.GetNumAdChans(Count)
                cnf.GetAdResolution(Resolution)
                WandlerMax = 2 ^ Resolution
                ReDim InputChannel(Count - 1)
                For i As Integer = 0 To InputChannel.Count - 1
                    InputChannel(i) = New Channel("RedLab2416_4AO AIn_" & i + 1)
                Next
            Case USBDevice.RedLab3103
                cnf.GetNumDaChans(Count)
                cnf.GetDacResolution(Resolution)
                WandlerMax = 2 ^ Resolution
                ReDim OutputChannel(Count - 1)
                For i As Integer = 0 To OutputChannel.Count - 1
                    OutputChannel(i) = New Channel("RedLab3103 AOut" & i + 1)
                Next
        End Select
    End Sub

    Public Sub Clear()
        For i As Integer = 0 To InputChannel.Count - 1
            InputChannel(i).data.Clear()
        Next i
    End Sub

    Public Sub MeasureAll()
        For Each c As Channel In InputChannel
            Select Case DeviceType
                Case USBDevice.MeasurementComputing_USBTemp
                    c.data.Add(GetTemperature(c.Index))
                Case USBDevice.RedLab2416_4AO
                    c.data.Add(GetAnalogInput(c.Index))
                Case USBDevice.RedLab3103
                    'nur digital möglich c.data.Add(GetAnalogInput(c.Index))
            End Select
        Next
    End Sub

    Private Function GetVoltage(ChannelIndex As Integer) As datapoint
        Dim Value As Single, dataValue As Integer
        Try
            DaqBoard.AIn32(ChannelIndex, MccDaq.Range.Bip10Volts, dataValue, 0)
            Value = (dataValue / WandlerMax)
        Catch ex As Exception
            Value = -1
        End Try
        Return New datapoint(Date.Now, Value)
    End Function

    Private Function GetTemperature(ChannelIndex As Integer) As datapoint
        Dim Value As Single
        Try
            DaqBoard.TIn(ChannelIndex, MccDaq.TempScale.Celsius, Value, MccDaq.ThermocoupleOptions.NoFilter)
        Catch ex As Exception
            Value = -9000
        End Try
        Return New datapoint(Date.Now, Value)
    End Function

    Public Function GetAnalogInput(ChannelIndex As Integer) As datapoint
        Dim Value As Double, dataValue As Integer
        DaqBoard.AIn32(ChannelIndex, MccDaq.Range.Bip10Volts, dataValue, 0)
        Value = (dataValue / WandlerMax)
        Return New datapoint(Date.Now, Value)
    End Function

    Public Sub SetAnalogOutput(ChannelIndex As Integer, Value As Double)
        Dim dp As New datapoint(Value)
        InputChannel(ChannelIndex).data.Add(dp)
        Dim DataVAlue As Short = Value * 255
        DaqBoard.AOut(ChannelIndex, MccDaq.Range.Bip10Volts, DataVAlue)
    End Sub

    Public Sub Export(FileName As String)
        Dim xml As New XmlSerializer(GetType(USBDevice))
        Using fs As New FileStream(IO.Path.ChangeExtension(FileName, ".xml"), FileMode.Create)
            xml.Serialize(fs, Me)
            fs.Close()
        End Using
    End Sub

    Public Shared Function Load(ByVal filename As String) As USBDevice
        Dim xml As New XmlSerializer(GetType(USBDevice))
        Using fs As New FileStream(filename, FileMode.Open)
            Dim q As USBDevice = xml.Deserialize(fs)
            fs.Close()
            Return q
        End Using
    End Function

End Class

Public Class Channel
    Public Connected As Boolean = True
    Public data As New List(Of datapoint)
    Public Name As String
    Public Einheit As String
    Public Index As Integer

    Public Sub New()
    End Sub
    Public Sub New(Name As String)
        Me.Name = Name
    End Sub

End Class

Public Class datapoint
    Public MeasurementDate As Date
    Public Value As Double
    Public Sub New()
    End Sub
    Public Sub New(Value As Double)
        MeasurementDate = Date.Now
        Me.Value = Value
    End Sub

    Public Sub New(meas_date As Date, value As Double)
        Me.MeasurementDate = meas_date 'New Date(meas_date.Year, meas_date.Month, meas_date.Day, meas_date.Hour, meas_date.Minute, meas_date.Second)
        Me.Value = value
    End Sub

End Class
