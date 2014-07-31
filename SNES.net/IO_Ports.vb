﻿Module IO_Ports
    Private Structure DMA_Channel
        Dim Control As Byte
        Dim Dest As Byte
        Dim Source_Bank As Byte
        Dim Source As Integer
        Dim Size As Integer
        Dim HDMA_Bank As Byte
    End Structure
    Private Structure HDMA_Channel
        Dim Source_Bank As Byte
        Dim Source As Integer
        Dim Count, Repeat As Integer
        Dim First As Boolean
        Dim Data() As Byte
    End Structure
    Dim DMA_Channels(7) As DMA_Channel
    Dim HDMA_Channels(7) As HDMA_Channel
    Dim HDMA_Enabled As Byte 'Canais ativados para transferência de DMA

    Public NMI_Enable As Boolean
    Dim Multiplicand, Multiplier, Divisor As Byte
    Dim Mult_Result, Div_Result, Dividend As Integer
    Public Sub Init_IO()
        For Channel = 0 To 7
            ReDim HDMA_Channels(Channel).Data(3)
        Next
    End Sub
    Public Function Read_IO(Address As Integer) As Byte
        Select Case Address
            Case &H4214 : Return Div_Result And &HFF
            Case &H4215 : Return (Div_Result And &HFF00) / &H100
            Case &H4216 : Return Mult_Result And &HFF
            Case &H4217 : Return (Mult_Result And &HFF00) / &H100
            Case &H4300, &H4310, &H4320, &H4330, &H4340, &H4350, &H4360, &H4370 : Return DMA_Channels((Address >> 4) And 7).Control
            Case &H4301, &H4311, &H4321, &H4331, &H4341, &H4351, &H4361, &H4371 : Return DMA_Channels((Address >> 4) And 7).Dest
            Case &H4302, &H4312, &H4322, &H4332, &H4342, &H4352, &H4362, &H4372 : Return DMA_Channels((Address >> 4) And 7).Source And &HFF
            Case &H4303, &H4313, &H4323, &H4333, &H4343, &H4353, &H4363, &H4373 : Return (DMA_Channels((Address >> 4) And 7).Source >> 8) And &HFF
            Case &H4304, &H4314, &H4324, &H4334, &H4344, &H4354, &H4364, &H4374 : Return DMA_Channels((Address >> 4) And 7).Source_Bank
            Case &H4305, &H4315, &H4325, &H4335, &H4345, &H4355, &H4365, &H4375 : Return DMA_Channels((Address >> 4) And 7).Size And &HFF
            Case &H4306, &H4316, &H4326, &H4336, &H4346, &H4356, &H4366, &H4376 : Return (DMA_Channels((Address >> 4) And 7).Size >> 8) And &HFF
        End Select

        Return Nothing 'Nunca deve acontecer
    End Function
    Public Sub Write_IO(Address As Integer, Value As Byte)
        Select Case Address And &H1FF
            Case &H0 : NMI_Enable = Value And &H80
            Case &H2 : Multiplicand = Value
            Case &H3
                Multiplier = Value
                Mult_Result = Multiplicand * Multiplier
            Case &H4 : Dividend = Value + (Dividend And &HFF00)
            Case &H5 : Dividend = (Value * &H100) + (Dividend And &HFF)
            Case &H6
                Divisor = Value
                If Not Dividend Or Not Divisor Then
                    Div_Result = &HFFFF
                    Mult_Result = Dividend
                Else
                    Div_Result = Dividend / Divisor
                    Mult_Result = Dividend Mod Divisor
                End If
            Case &HB 'Transferência de DMA
                For Channel As Integer = 0 To 7
                    If Value And (1 << Channel) Then 'Verifica se deve transferir
                        With DMA_Channels(Channel)
                            Dim Original_Dest As Integer = .Dest Or &H2100

                            If .Size = 0 Then .Size = &H10000
                            While .Size
                                If .Control And &H80 Then
                                    Write_Memory(.Source_Bank, .Source, Read_Memory(0, &H2100 Or .Dest))
                                Else
                                    Write_Memory(0, &H2100 Or .Dest, Read_Memory(.Source_Bank, .Source))
                                End If
                                Select Case .Control And &HF
                                    Case 0, 2 : If .Control And &H10 Then .Source -= 1 Else .Source += 1
                                    Case 1
                                        If .Dest Or &H2100 = Original_Dest Then .Dest += 1 Else .Dest -= 1
                                        If .Control And &H10 Then .Source -= 1 Else .Source += 1
                                    Case 9 : If .Dest Or &H2100 = Original_Dest Then .Dest += 1 Else .Dest -= 1
                                End Select
                                .Size -= 1
                            End While
                        End With
                    End If
                Next
            Case &HC : If Value > 0 Then MsgBox(Hex(Value)) : HDMA_Enabled = Value
            Case &H100, &H110, &H120, &H130, &H140, &H150, &H160, &H170 : DMA_Channels((Address >> 4) And 7).Control = Value
            Case &H101, &H111, &H121, &H131, &H141, &H151, &H161, &H171 : DMA_Channels((Address >> 4) And 7).Dest = Value
            Case &H102, &H112, &H122, &H132, &H142, &H152, &H162, &H172 'High Byte de leitura
                With DMA_Channels((Address >> 4) And 7)
                    .Source = (.Source And &HFF00) Or Value
                End With
            Case &H103, &H113, &H123, &H133, &H143, &H153, &H163, &H173 'Low Byte de leitura
                With DMA_Channels((Address >> 4) And 7)
                    .Source = (.Source And &HFF) Or (Value << 8)
                End With
            Case &H104, &H114, &H124, &H134, &H144, &H154, &H164, &H174 : DMA_Channels((Address >> 4) And 7).Source_Bank = Value
            Case &H105, &H115, &H125, &H135, &H145, &H155, &H165, &H175 'High Byte do tamanho
                With DMA_Channels((Address >> 4) And 7)
                    .Size = (.Size And &HFF00) Or Value
                End With
            Case &H106, &H116, &H126, &H136, &H146, &H156, &H166, &H176 'Low Byte do tamanho
                With DMA_Channels((Address >> 4) And 7)
                    .Size = (.Size And &HFF) Or (Value << 8)
                End With
            Case &H107, &H117, &H127, &H137, &H147, &H157, &H167, &H177 : DMA_Channels((Address >> 4) And 7).HDMA_Bank = Value
        End Select
    End Sub
    Public Sub H_Blank_DMA(Scanline As Integer)
        For Channel As Integer = 0 To 7
            With HDMA_Channels(Channel)
                If Scanline = 0 Then 'Novo Frame
                    .Source = DMA_Channels(Channel).Source
                    .Source_Bank = DMA_Channels(Channel).Source_Bank
                    .Count = 0
                End If

                If HDMA_Enabled And (1 << Channel) Then 'Verifica se deve transferir
                    MsgBox("DMA Ocurred!")
                    '+===========================+
                    '| Carrega valores da tabela |
                    '+===========================+

                    If Not (.Count And &H7F) Then
                        .Count = Read_Memory(.Source_Bank, .Source)
                        .Source += 1
                        .Repeat = .Count And &H80
                        .Count = .Count And &H7F

                        Select Case DMA_Channels(Channel).Control And &H47
                            Case 0 'Modo Normal
                                .Data(0) = Read_Memory(.Source_Bank, .Source)
                                .Source += 1
                            Case 1, 2
                                .Data(0) = Read_Memory(.Source_Bank, .Source)
                                .Data(1) = Read_Memory(.Source_Bank, .Source + 1)
                                .Source += 2
                            Case 3, 4
                                .Data(0) = Read_Memory(.Source_Bank, .Source)
                                .Data(1) = Read_Memory(.Source_Bank, .Source + 1)
                                .Data(2) = Read_Memory(.Source_Bank, .Source + 2)
                                .Data(3) = Read_Memory(.Source_Bank, .Source + 3)
                                .Source += 4
                            Case &H40 'Modo Indireto
                                Dim Address As Integer = Read_Memory_16(.Source_Bank, .Source)
                                .Data(0) = Read_Memory(DMA_Channels(Channel).HDMA_Bank, Address)
                                .Source += 2
                            Case &H41, &H42
                                Dim Address As Integer = Read_Memory_16(.Source_Bank, .Source)
                                .Data(0) = Read_Memory(DMA_Channels(Channel).HDMA_Bank, Address)
                                .Data(1) = Read_Memory(DMA_Channels(Channel).HDMA_Bank, Address + 1)
                                .Source += 2
                            Case &H43, &H44
                                Dim Address As Integer = Read_Memory_16(.Source_Bank, .Source)
                                .Data(0) = Read_Memory(DMA_Channels(Channel).HDMA_Bank, Address)
                                .Data(1) = Read_Memory(DMA_Channels(Channel).HDMA_Bank, Address + 1)
                                .Data(2) = Read_Memory(DMA_Channels(Channel).HDMA_Bank, Address + 2)
                                .Data(3) = Read_Memory(DMA_Channels(Channel).HDMA_Bank, Address + 3)
                                .Source += 2
                        End Select
                        .First = True
                    End If

                    '+=================+
                    '| Escreve valores |
                    '+=================+

                    If .First Or .Repeat Then
                        .First = False
                        Select Case DMA_Channels(Channel).Control And &H7
                            Case 0 : Write_Memory(0, &H2100 Or DMA_Channels(Channel).Dest, .Data(0))
                            Case 1
                                Write_Memory(0, &H2100 Or DMA_Channels(Channel).Dest, .Data(0))
                                Write_Memory(0, &H2100 Or (DMA_Channels(Channel).Dest + 1), .Data(1))
                            Case 2
                                Write_Memory(0, &H2100 Or DMA_Channels(Channel).Dest, .Data(0))
                                Write_Memory(0, &H2100 Or DMA_Channels(Channel).Dest, .Data(1))
                            Case 3
                                Write_Memory(0, &H2100 Or DMA_Channels(Channel).Dest, .Data(0))
                                Write_Memory(0, &H2100 Or (DMA_Channels(Channel).Dest + 1), .Data(1))
                                Write_Memory(0, &H2100 Or DMA_Channels(Channel).Dest, .Data(2))
                                Write_Memory(0, &H2100 Or (DMA_Channels(Channel).Dest + 1), .Data(3))
                            Case 4
                                Write_Memory(0, &H2100 Or DMA_Channels(Channel).Dest, .Data(0))
                                Write_Memory(0, &H2100 Or (DMA_Channels(Channel).Dest + 1), .Data(1))
                                Write_Memory(0, &H2100 Or (DMA_Channels(Channel).Dest + 2), .Data(2))
                                Write_Memory(0, &H2100 Or (DMA_Channels(Channel).Dest + 3), .Data(3))
                        End Select
                    End If

                    .Count -= 1
                End If
            End With
        Next
    End Sub
End Module