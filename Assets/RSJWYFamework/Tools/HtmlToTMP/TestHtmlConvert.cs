using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class TestHtmlConvert : MonoBehaviour
{
    public TextMeshProUGUI tmpText; // 引用你的 TMP 组件

    public string rawHtml;
    void Start()
    {
        // 模拟一段后端传来的 HTML 数据
        //string rawHtml = "<p><span style=\"font-size: 36px;\">山东省第三家、青岛市首家金融租赁公司，注册资本</span> <span style=\"font-size: 60px;\"><strong style=\"color: rgb(167, 237, 255);\">12.25 亿元</strong></span> 。</p>";
        rawHtml = "        八年深耕，青银金租累计投放资金超600亿元，服务客户超600家，制造业、水利环境和公共设施管理业、租赁与商务服务业三大领域占比六成，民营企业支持比例近半。依托<color=#c68710><size=30><b>“总行战略协同+地方精准渗透”</b></size></color>的双轮驱动模式和<color=#c68710><size=30><b>“营销+尽调+租后”</b></size></color>全链条管理体系，决策高效、服务稳健。☺좤蛸烙̀ປ2㍠";
        // 转换并赋值
        tmpText.text = HtmlToTmpConverter.Convert(rawHtml);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            tmpText.text = HtmlToTmpConverter.Convert(rawHtml);
        }
    }
}