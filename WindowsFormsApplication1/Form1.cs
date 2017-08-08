using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public class filemodel {
            public string path { set;get;}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //选择文件夹
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string path = folderBrowserDialog1.SelectedPath;
                textBox1.Text = path;
                BindData("*.aspx");
            }
            //Calendar cc = new Calendar(DateTime.Now);
            //label1.Text = "农历节日：" + cc.CalendarHoliday + "，公历节日：" + cc.DateHoliday + "，农历：" + cc.ChineseDateString + "，节气：" + cc.ChineseTwentyFourDay + "，公历日期：" + cc.Date + "，是否闰年：" + cc.IsChineseLeapYear + "，" + cc.WeekDayHoliday;
                
        }

        private void BindData(string searchtype) {
            if (!string.IsNullOrEmpty(textBox1.Text.Trim())) {
                IEnumerable<string> files = Directory.EnumerateFiles(textBox1.Text, searchtype, SearchOption.TopDirectoryOnly);
                List<filemodel> myfile = new List<filemodel>();
                filemodel myfilemodel = new filemodel();
                foreach (string model in files)
                {
                    myfilemodel = new filemodel();
                    myfilemodel.path = model;
                    myfile.Add(myfilemodel);
                }
                GridViewFileShow.DataSource = myfile;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackColor = Color.Red;
            this.TransparencyKey = BackColor;
            this.Opacity = 1;
            this.comboBox1.Text = "*.aspx";
            this.comboBox1.Items.AddRange(new object[] {"*.aspx","*.html"});
            //this.comboBox1.SelectionStart = 1;
        }
        //设置窗口透明度
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.Opacity = Math.Round((Double)(12-trackBar1.Value) / (Double)10, 1);
        }
        
        //搜索框内容改变时
        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text != null) {
                BindData(comboBox1.Text);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //分析文件
            DataGridViewCheckBoxCell check = new DataGridViewCheckBoxCell();
            List<string> pathlist = new List<string>();
            foreach (DataGridViewRow row in GridViewFileShow.Rows) {
                check = (DataGridViewCheckBoxCell)row.Cells[0];
                if ((bool)check.EditedFormattedValue || (bool)check.FormattedValue) {
                    pathlist.Add(row.Cells[1].Value.ToString());
                }
            }
            progressBar1.Maximum = pathlist.Count;
            progressBar1.Step = 1;
            foreach(string path in pathlist){
                GetFileCssJSLink(path,"js");
                progressBar1.PerformStep();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //处理文件    需要的参数   文件路径、正则类型
            StringBuilder strhtml = ReadHtmlFile("");
            Regex reg = new Regex("(src=\")(.*\\.js.*)(\")");
            MatchCollection matches = reg.Matches(strhtml.ToString(), 0);
            foreach (Match math in matches)
            {
                if (math.Groups[2].Value.IndexOf('?') > 0)
                {
                    string old = math.Groups[2].Value;
                    old = old.Substring(0, math.Groups[2].Value.IndexOf('?'));
                    strhtml.Replace(math.Groups[2].Value, old + "?v=" + DateTime.Now.Ticks);
                }
                else
                {
                    strhtml.Replace(math.Groups[2].Value, math.Groups[2].Value + "?v=" + DateTime.Now.Ticks);
                }
            }
            
            //WriteHtmlFile(strhtml, "D:\\发布web\\editlink\\" + path.Split('\\').Last());
        }
        
        /// <summary>
        /// 获取文件的链接  文件中所有的链接
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="type">链接类型</param>
        /// <returns></returns>
        private List<string> GetFileCssJSLink(string path,string type)
        {
            List<string> result = new List<string>();
            StringBuilder strhtml = ReadHtmlFile(path);
            string regx = string.Empty;
            if (type == "js"){
                regx = "(src=\")(.*\\.js.*)(\")";
                Regex reg = new Regex(regx);
                MatchCollection matches = reg.Matches(strhtml.ToString(), 0);
                foreach (Match math in matches)
                {
                    result.Add(math.Groups[2].Value);
                }
            }
            else if (type == "css"){
                regx = "(href=\")(.*\\.css.*)(\")";
                Regex reg = new Regex(regx);
                MatchCollection matches = reg.Matches(strhtml.ToString(), 0);
                foreach (Match math in matches)
                {
                    result.Add(math.Groups[2].Value);
                }
            }
            else {
                regx = "((href=\")|(src=\"))((.*\\.css.*)|(.*\\.js.*))(\")";
                Regex reg = new Regex(regx);
                MatchCollection matches = reg.Matches(strhtml.ToString(), 0);
                foreach (Match math in matches)
                {
                    result.Add(math.Groups[4].Value);
                }
            }
            return result;
        }

        

        private void historyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //菜单历史记录  需要的参数   文件路径、正则类型、替换的链接
            MessageBox.Show("暂未完成该功能！");
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="temp"></param>Server.MapPath(".") + "/location_new.html")
        /// <returns></returns>
        public StringBuilder ReadHtmlFile(string temppath)
        {
            StringBuilder temp = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(temppath))
                {
                    temp.Append(sr.ReadToEnd());
                    sr.Dispose();
                    sr.Close();
                }
            }
            catch (Exception exp)
            {

            }
            return temp;
        }

        /// <summary>
        /// 写入HTML文件
        /// </summary>"F:/LIEZHONG2.0/LIEZHONGV2.Web/Common/test.htm"
        /// <param name="str">HTML代码</param>
        /// <param name="htmlfilename">完整带路径的文件名</param>
        /// <returns></returns>
        public bool WriteHtmlFile(StringBuilder str, string htmlfilename)
        {
            // 写文件 
            try
            {
                using (StreamWriter sw = new StreamWriter(htmlfilename, false, System.Text.Encoding.GetEncoding("UTF-8"))) //保存地址
                {
                    sw.WriteLine(str);
                    sw.Flush();
                    sw.Dispose();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {

            }
            return true;
        }
    }
}
