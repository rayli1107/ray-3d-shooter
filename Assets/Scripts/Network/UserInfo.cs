using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class UserInfoList
{
    public List<UserInfo> users;
    public UserInfoList()
    {
        users = new List<UserInfo>();
    }
}

[Serializable]
public class UserInfo
{
    public string email;
    public string password;
    public string playerName;
}

[Serializable]
public class PlayerTransform
{
    public float x;
    public float z;
    public float rotation_x;
    public float rotation_y;
    public float rotation_z;
}
