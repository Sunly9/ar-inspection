using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inspection
{
    /// <summary>与 Go 后端约定的数据结构，字段名必须和 JSON 完全一致（小驼峰）。</summary>
    [Serializable]
    public class PositionDto
    {
        public float x;
        public float y;
        public float z;

        public PositionDto() { }

        public PositionDto(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);

        public static PositionDto From(Vector3 v) => new PositionDto(v.x, v.y, v.z);

        public override string ToString() => string.Format("({0:F2}, {1:F2}, {2:F2})", x, y, z);
    }

    /// <summary>POST /api/issues 的请求体：只带客户端该给的字段。</summary>
    [Serializable]
    public class CreateIssueRequest
    {
        public string title;
        public string description;
        public string priority;
        public PositionDto position;
    }

    /// <summary>后端返回的完整问题对象。</summary>
    [Serializable]
    public class IssueDto
    {
        public string id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public PositionDto position;
        public string createdAt;
        public string updatedAt;
    }

    // 下面几个包装类是为了让 JsonUtility 能解析 {"data": ...} 这种对象信封
    // （JsonUtility 不支持顶层数组，所以后端返回的是 {"data":{"items":[...]}}）
    [Serializable] public class IssueEnvelope { public IssueDto data; }
    [Serializable] public class IssueListEnvelope { public IssueListData data; }
    [Serializable] public class IssueListData { public List<IssueDto> items; }
    [Serializable] public class ErrorEnvelope { public ErrorBody error; }
    [Serializable] public class ErrorBody { public string code; public string message; }
}
