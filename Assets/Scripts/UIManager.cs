using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Net.Mail;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
  // Start is called before the first frame update

  [SerializeField] private RawImage _previewImage;

  [SerializeField] private Canvas _uiCanvas;

  [SerializeField] private Button _sendEmailButton;

  [SerializeField] private InputField _emailField;

  private bool _isSendingEmail;

  private byte[] _photoData;

  private string _imagePath;
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }

  public void OnCaptureStart()
  {
    _uiCanvas.gameObject.SetActive(false);


    StartCoroutine(Capture());
  }
  private IEnumerator Capture()
  {
    yield return new WaitForEndOfFrame();
    Debug.Log("Capturing photo");

    Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
    //Read the pixels in the Rect starting at 0,0 and ending at the screen's width and height hiiii

    texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
    texture.Apply();

    byte[] bytes = texture.EncodeToPNG();

    _previewImage.texture = texture;
    _previewImage.gameObject.SetActive(true);

    _emailField.gameObject.SetActive(true);
    _sendEmailButton.gameObject.SetActive(true);


    // Destroy(image);

    // Debug.Log(bytes);

    File.WriteAllBytes(Application.persistentDataPath + "/" + "capture.png", bytes);
  }

  public void SendEmail()
  {
    string emailAddress = _emailField.text;

    Debug.Log("email address: " + emailAddress);

    // do not email if field is empty
    if (string.IsNullOrEmpty(emailAddress)) return;

    // assemble email 
    MailMessage message = new MailMessage(
        "sam@emailzz.net",
        emailAddress,
        "Your AR photo!",
        "Your photo is attached to this email. Thanks for visiting!");

    byte[] captureData = File.ReadAllBytes(Application.persistentDataPath + "/" + "capture.png");

    Attachment photoAttachment = new Attachment(Application.persistentDataPath + "/" + "capture.png");
    message.Attachments.Add(photoAttachment);
    // add attachement

    // create server
    SmtpClient mailClient = new SmtpClient("smtp.gmail.com");
    mailClient.Port = 587;
    mailClient.EnableSsl = true;
    // very much not safe at ALL
    mailClient.Credentials = new System.Net.NetworkCredential("bsamjones@gmail.com", "daphneisthebestcat");

    Debug.Log("sending email");

    mailClient.SendMailAsync(message);

    mailClient.SendCompleted += (sender, args) =>
    {
      _previewImage.gameObject.SetActive(false);

      _emailField.gameObject.SetActive(false);
      _sendEmailButton.gameObject.SetActive(false);
      _uiCanvas.gameObject.SetActive(true);
    };



  }

  public void loadScene(string sceneName)
  {
    SceneManager.LoadSceneAsync(sceneName);

    // if (sceneName == "DelayFrame")
    // {
    //   SceneManager.UnloadSceneAsync("FreezeFrame");
    // }
    // else
    // {
    //   SceneManager.UnloadSceneAsync("DelayFrame");
    // }
  }
}
