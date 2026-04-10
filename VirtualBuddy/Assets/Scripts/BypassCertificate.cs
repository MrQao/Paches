using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // ?? 这里直接返回 true，表示“跳过所有证书验证”（不推荐用于上线）
        return true;
    }
}
