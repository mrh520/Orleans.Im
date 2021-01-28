using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Im
{
    public class ApiResult<T>
    {
        public int Code { get; set; }

        public string Msg { get; set; } = "";

        public T Data { get; set; }
    }

    public class IM_List
    {
        public IM_User mine { get; set; }

        public List<IM_Friend> friend { get; set; }

        public List<IM_Group> group { get; set; }
    }


    public class IM_Member
    {
        public IM_User owner { get; set; }

        public int members { get; set; }

        public List<IM_User> list { get; set; }
    }

    public class IM_User
    {
        public string username { get; set; }
        public string id { get; set; }
        public string status { get; set; }
        public string sign { get; set; }
        public string avatar { get; set; }
    }

    public class IM_Friend
    {
        public string groupname { get; set; }
        public int id { get; set; }
        public int online { get; set; }
        public List<IM_User> list { get; set; }
    }

    public class IM_Group
    {
        public string groupname { get; set; }
        public string id { get; set; }
        public string avatar { get; set; }
    }
}
