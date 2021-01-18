using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Im.Common
{
    public class Packet
    {
        /// <summary>
        /// 接收id
        /// </summary>
        public string ReceiveId { get; set; }

        /// <summary>
        /// 发送id
        /// </summary>
        public string SendId { get; set; }

        /// <summary>
        /// 0表示发单 1表示群发
        /// </summary>
        public int SendType { get; set; }

        /// <summary>
        /// 0表示文本 1表示图片 2表示语音 3表示视频 4表示地图
        /// </summary>
        public ChatContentType ContentType { get; set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息发送时间
        /// </summary>
        public DateTime SendDate { get; set; }

        /// <summary>
        /// 群聊名称
        /// </summary>
        public string ChanName { get; set; }
    }

    public enum ChatContentType
    {
        /// <summary>
        /// 文本
        /// </summary>
        Text,
        /// <summary>
        /// 图片
        /// </summary>
        Image,
        /// <summary>
        /// 音频
        /// </summary>
        Viedo,
        /// <summary>
        /// 视频
        /// </summary>
        Audio,
        /// <summary>
        /// 地图
        /// </summary>
        Map
    }
}
