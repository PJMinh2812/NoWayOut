# THIẾT KẾ GAMEPLAY: **NO WAY OUT -- RAGE GAME EDITION**

**Thể loại:** Rage Game / Masocore Puzzle Platformer\
**Triết lý thiết kế:** *"Trust No One" -- Đừng tin bất cứ thứ gì bạn
thấy*

Trong phiên bản này, kẻ thù chính không phải quái vật mà là **sự kỳ vọng
của người chơi**. Trò chơi được thiết kế để đánh lừa, gây bất ngờ và
trừng phạt sai lầm bằng cơ chế thử--sai (trial & error).
fileciteturn1file0

------------------------------------------------------------------------

## I. Triết Lý Thiết Kế Cốt Lõi

**Quy tắc vàng:**\
\> *Những gì trông an toàn nhất thường là nơi nguy hiểm nhất.*
fileciteturn1file0

Người chơi phải: - Ghi nhớ vị trí bẫy\
- Học từ cái chết\
- Tiến bộ nhờ kinh nghiệm thay vì chỉ kỹ năng phản xạ

------------------------------------------------------------------------

## II. Kho Vũ Khí Gây Ức Chế (Frustration Arsenal)

### 1. Bẫy Phá Điều Khiển (Movement Sabotage)

#### 🧊 Sàn Băng / Dầu (Slippery Tiles)

-   Bước vào → trượt 3--4 ô mới dừng\
-   Thường đặt sát bẫy hoặc vực\
-   Khiến người chơi tính toán đúng nhưng vẫn chết oan
    fileciteturn1file0

#### 🔄 Gạch Đảo Ngược (Confusion Rune)

-   Đảo ngược điều khiển trong 5 giây (W⇄S, A⇄D)\
-   Đặt ngay trước đoạn nhảy quan trọng\
-   Phản xạ quen tay khiến người chơi tự lao xuống hố
    fileciteturn1file0

#### 🌬️ Gió Thổi Ngược (Pushback Wind)

-   Cứ 3 giây có một luồng gió mạnh\
-   Nếu đang nhảy → bị lệch quỹ đạo → rơi\
-   Tạo cảm giác "chết vì game troll" thay vì do lỗi bản thân
    fileciteturn1file0

------------------------------------------------------------------------

### 2. Bẫy Troll Kiểu *Trap Adventure*

#### 🧱 Gạch Tàng Hình (Invisible Block)

-   Người chơi nhảy qua hố\
-   Đầu đập vào khối vô hình giữa không trung\
-   Rơi xuống và chết ngay lập tức fileciteturn1file0

#### 🕳️ Gạch Rơi Giả (Fake Floor)

-   Nhìn giống sàn thường\
-   Dẫm vào là biến mất ngay, không cảnh báo\
-   Buộc người chơi phải chết thử để ghi nhớ fileciteturn1file0

#### ⬇️ Gai Mọc Ngược (Ceiling Spikes)

-   Vừa đáp lên bục hoặc mở rương\
-   Gai từ trần rơi xuống hoặc bật ra từ vật phẩm\
-   Trừng phạt lòng tham của người chơi fileciteturn1file0

------------------------------------------------------------------------

### 3. Cơ Chế "Only Up" -- Trừng Phạt Tiến Độ

#### 🏗️ Màn Chơi Nhiều Tầng (Layered Dungeon)

-   Map xếp tầng dọc\
-   Trượt chân ở tầng cao → rơi xuống tầng thấp\
-   Không chết, nhưng mất nhiều phút leo lại\
-   Gây ức chế kiểu *Getting Over It* fileciteturn1file0

#### 🦇 Quái Vật Đẩy Lùi (Knockback Enemies)

-   Gây ít sát thương nhưng đẩy lùi cực mạnh\
-   Một cú đụng nhẹ có thể khiến người chơi rơi khỏi cầu thang hẹp\
-   Tạo cảm giác bất lực hơn là bị hạ gục trực tiếp
    fileciteturn1file0

------------------------------------------------------------------------

## III. Kịch Bản Gameplay Mẫu Gây Ức Chế

**Bối cảnh:** Một chiếc rương vàng ở bên kia vực sâu, có các bậc đá bay
để nhảy qua. fileciteturn1file0

1.  Nhảy lên bậc đầu → an toàn\
2.  Nhảy sang bậc hai → bậc nứt, buộc nhảy vội\
3.  Bậc ba trông chắc chắn → mũi tên bắn bật ngược → rơi xuống vực

**Lần chơi lại:** Người chơi né được mũi tên, tới được rương.

4.  Mở rương → rương trống + lò xo bật ngược về điểm xuất phát

**Lối đi thật:** Đi xuyên qua bức tường giả phía sau rương.
fileciteturn1file0

------------------------------------------------------------------------

## IV. Gợi Ý Kỹ Thuật (Unity / C#)

### Invisible Block

``` csharp
public class InvisibleBlock : MonoBehaviour
{
    private Renderer _renderer;
    private Collider _collider;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();
        _renderer.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.contacts[0].normal.y > 0)
            {
                _renderer.enabled = true;
                PlayBonkSound();
            }
        }
    }
}
```

### Fake Floor

\`\`\`csharp public class FakeFloor : MonoBehaviour { private void
OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) {
GetComponent`<Collider>`{=html}().enabled = false;
GetComponent`<Renderer>`{=html}().enabled = false; Invoke("ResetFloor",
5f); } }

    void ResetFloor()
    {
        GetComponent<Collider>().enabled = true;
        GetComponent<Renderer>().enabled = true;
    }

} \`\`\` fileciteturn1file0

------------------------------------------------------------------------

## V. Tổng Kết

Thiết kế này tạo ra trải nghiệm: - 🎯 **Puzzle bằng ký ức**\
- ⚡ **Action đòi hỏi phản xạ nhanh**\
- 😈 **Ức chế có chủ đích nhưng công bằng sau khi đã học bài**

Người chơi chiến thắng không nhờ may mắn, mà nhờ **ghi nhớ những lần
chết trước đó**. fileciteturn1file0
