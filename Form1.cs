using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;  // IP，IPAddress, IPEndPoint，端口等；
using System.Threading;
using System.IO;
using System.Data.SqlClient;
using MySql.Data.MySqlClient; 

namespace _11111
{
    public partial class frm_server : Form
    {
        public frm_server()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }

        Thread threadWatch = null; // 负责监听客户端连接请求的 线程；
        Socket socketWatch = null;

        Dictionary<string, Socket> dict = new Dictionary<string, Socket>();
        Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();

        private void btnBeginListen_Click(object sender, EventArgs e)
        {
            // 创建负责监听的套接字，注意其中的参数；
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 获得文本框中的IP对象；
            IPAddress address = IPAddress.Parse(txtIp.Text.Trim());
                // 创建包含ip和端口号的网络节点对象；
                IPEndPoint endPoint = new IPEndPoint(address, int.Parse(txtPort.Text.Trim()));
                try
                {
                    // 将负责监听的套接字绑定到唯一的ip和端口上；
                    socketWatch.Bind(endPoint);
                }
                catch (SocketException se)
                {
                    MessageBox.Show("异常："+se.Message);
                    return;
                }
                // 设置监听队列的长度；
                socketWatch.Listen(10);
                // 创建负责监听的线程；
                threadWatch = new Thread(WatchConnecting);
                threadWatch.IsBackground = true;
                threadWatch.Start();
                ShowMsg("服务器启动监听成功！");
            //}
        }

        /// <summary>
        /// 监听客户端请求的方法；
        /// </summary>
        void WatchConnecting()
        {
            while (true)  // 持续不断的监听客户端的连接请求；
            {
                // 开始监听客户端连接请求，Accept方法会阻断当前的线程；
                Socket sokConnection = socketWatch.Accept(); // 一旦监听到一个客户端的请求，就返回一个与该客户端通信的 套接字；
                // 想列表控件中添加客户端的IP信息；
                lbOnline.Items.Add(sokConnection.RemoteEndPoint.ToString());
                // 将与客户端连接的 套接字 对象添加到集合中；
                dict.Add(sokConnection.RemoteEndPoint.ToString(), sokConnection);
                ShowMsg("客户端连接成功！");
                Thread thr = new Thread(RecMsg);
                thr.IsBackground = true;
                thr.Start(sokConnection);
                dictThread.Add(sokConnection.RemoteEndPoint.ToString(), thr);  //  将新建的线程 添加 到线程的集合中去。
            }
        }

        void RecMsg(object sokConnectionparn)
        {
                byte[] ackMsgOK = System.Text.Encoding.Default.GetBytes("Y");
                byte[] ackMsgERR = System.Text.Encoding.Default.GetBytes("error msg!");
                Socket sokClient = sokConnectionparn as Socket;
                while (true)
                {
                    // 定义一个128的缓存区；
                    byte[] arrMsgRec = new byte[128];
                    // 将接受到的数据存入到输入  arrMsgRec中；
                    int length = -1;
                    try
                    {
                        length = sokClient.Receive(arrMsgRec); // 接收数据，并返回数据的长度；
                    }
                    catch (SocketException se)
                    {
                        ShowMsg("异常：" + se.Message);
                        // 从 通信套接字 集合中删除被中断连接的通信套接字；
                        dict.Remove(sokClient.RemoteEndPoint.ToString());
                        // 从通信线程集合中删除被中断连接的通信线程对象；
                        dictThread.Remove(sokClient.RemoteEndPoint.ToString());
                        // 从列表中移除被中断的连接IP
                        lbOnline.Items.Remove(sokClient.RemoteEndPoint.ToString());
                        break;
                    }
                    catch (Exception e)
                    {
                        ShowMsg("异常：" + e.Message);
                        // 从 通信套接字 集合中删除被中断连接的通信套接字；
                        dict.Remove(sokClient.RemoteEndPoint.ToString());
                        // 从通信线程集合中删除被中断连接的通信线程对象；
                        dictThread.Remove(sokClient.RemoteEndPoint.ToString());
                        // 从列表中移除被中断的连接IP
                        lbOnline.Items.Remove(sokClient.RemoteEndPoint.ToString());
                        break;
                    }

                    string strMsg = ByteToHex(arrMsgRec,length); // 将接受到的字节数据转化成字符串；
                    ShowMsg(strMsg);
                    if ((arrMsgRec[0] == 0x3A) && (arrMsgRec[1] == 0x5C) && (arrMsgRec[2] == 0x3E) && (arrMsgRec[22] == 0x05) && (arrMsgRec[23] == 0xdc))// 表示接收到的是有效数据；
                    {
                        byte[] dtaddress = new byte[4];
                        byte[] dtcode = new byte[4];
                        string idstring = System.Text.Encoding.UTF8.GetString(arrMsgRec, 12, 10);// 将接受到的字节数据转化成字符串；
                        Buffer.BlockCopy(arrMsgRec, 4, dtaddress, 0, 4);
                        int dterraddr = System.BitConverter.ToInt32(dtaddress, 0)+40000;
                        Buffer.BlockCopy(arrMsgRec, 8, dtcode , 0, 4);
                        int dterrcode = System.BitConverter.ToInt32(dtcode, 0);
                        
 //                       bool ifAutoflag ;
 //                       if (arrMsgRec[3] == 0) { ifAutoflag = true; }
 //                       else { ifAutoflag = false; }
                        string MyConn = "Data Source=localhost;Database=dianti_1;User Id=guest;Password=123456";//定义数据库连接参数
                        MySqlConnection MyConnection = new MySqlConnection(MyConn);//定义一个数据连接实例
                        MySqlDataAdapter myDataAdapter = new MySqlDataAdapter("select * from input", MyConnection);
                        DataSet myDataSet = new DataSet();
                        myDataAdapter.Fill(myDataSet);

                        DataTable myTable = myDataSet.Tables[0];
                        DataRow myRow = myTable.NewRow();

                        myRow["termid"] = 1001 ;
                        myRow["termaddress"] = "紫竹园6号楼" ;

                        int dataflag = 0;
                        switch (dterraddr)
                        {
                            case 40001: myRow["status"] = "40001:欠速故障";
                                break;
                            case 40002: myRow["status"] = "40002:超速故障";
                                break;
                            case 40003: myRow["status"] = "40003:反转故障";
                                break;
                            case 40004: myRow["descript"] = "40004:AST故障（失速）";
                                break;
                            case 40005: myRow["status"] = "40005:过电流";
                                break;
                            case 40006: myRow["status"] = "40006:过电压";
                                break;
                            case 40007: myRow["status"] = "40007:欠电压";
                                break;
                            case 40008: myRow["status"] = "40008:LB接触器故障";
                                break;
                            case 40009: myRow["status"] = "40009:接触器故障";
                                break;
                            case 40010: myRow["status"] = "40010:BK抱闸故障";
                                break;
                            default: dataflag =1;
                                break;
                        }

                        DateTime dt = DateTime.Now;
                        myRow["inputtime"] = dt;
 //                       myRow["ifauto"] = ifAutoflag;
                        if (dataflag == 0)
                        {
                            //myRow["id"] = 100; id若为“自动增长”，此处可以不设置，即便设置也无效
                            myTable.Rows.Add(myRow);

                            // 将DataSet的修改提交至“数据库”
                            MySqlCommandBuilder mySqlCommandBuilder = new MySqlCommandBuilder(myDataAdapter);
                            myDataAdapter.Update(myDataSet);
                            sokClient.Send(ackMsgOK); //向客户端返回成功信息
                        }
                        /*
                            foreach (DataRow myRow in myTable.Rows)
                            {
                                foreach (DataColumn myColumn in myTable.Columns)
                                {
                                    System.Diagnostics.Debug.WriteLine(myRow[myColumn]);	//遍历表中的每个单元格
                                }
                            }*/
//                        dataGridView1.DataSource = myDataSet.Tables[0];



                        myDataSet.Dispose();        // 释放DataSet对象
                        myDataAdapter.Dispose();    // 释放SqlDataAdapter对象
                        //            myDataReader.Dispose();     // 释放SqlDataReader对象
                        MyConnection.Close();             // 关闭数据库连接
                        MyConnection.Dispose();           // 释放数据库连接对象
                    }
                    else
                    {
//                        sokClient.Send(ackMsgERR);
                    }
                }     
        }

        void ShowMsg(string str)
        {
            txtMsg.AppendText(str + "\r\n");
        }

        public static string ByteToHex(byte[] comByte,int dataLen)
       {
            string returnStr = "";
            if (comByte != null)
            {
              for (int i = 0; i < dataLen; i++)
               {
                   returnStr += comByte[i].ToString("X2") + " ";
               }
            }
           return returnStr;   
       }


        // 发送消息
        private void btnSend_Click(object sender, EventArgs e)
        {
            string strMsg = txtMsgSend.Text.Trim();
            byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg); // 将要发送的字符串转换成Utf-8字节数组；
//            byte[] arrSendMsg=new byte[arrMsg.Length+1];
//            arrSendMsg[0] = 0; // 表示发送的是消息数据
//            Buffer.BlockCopy(arrMsg, 0, arrSendMsg, 1, arrMsg.Length);
            string strKey = "";
            strKey = lbOnline.Text.Trim();
            if (string.IsNullOrEmpty(strKey))   // 判断是不是选择了发送的对象；
            {
                MessageBox.Show("请选择你要发送的对象！！！");
            }
            else
            {
                dict[strKey].Send(arrMsg);// 解决了 sokConnection是局部变量，不能再本函数中引用的问题；
                ShowMsg(strMsg);
                txtMsgSend.Clear();
            }
        }

        /// <summary>
        /// 群发消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">消息</param>
        private void btnSendToAll_Click(object sender, EventArgs e)
        {
            string strMsg =  txtMsgSend.Text.Trim();
            byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg); // 将要发送的字符串转换成Utf-8字节数组；

//            byte[] arrSendMsg = new byte[arrMsg.Length + 1];
//            arrSendMsg[0] = 0; // 表示发送的是消息数据
//            Buffer.BlockCopy(arrMsg, 0, arrSendMsg, 1, arrMsg.Length);

            foreach (Socket s in dict.Values)
            {
                s.Send(arrMsg);
            }
            ShowMsg(strMsg);
            txtMsgSend.Clear();
            ShowMsg(" 群发完毕～～～");
        }
    
    }
}
