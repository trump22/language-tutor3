SET client_encoding = 'UTF8';

UPDATE "Lesson"
SET "content" = $json$
[
  {
    "type": "multiple-choice",
    "question": "Chọn câu chào đúng.",
    "options": ["你好！", "我忙饭。", "中文我。", "谢谢吗。"],
    "answer": "你好！",
    "explanation": "你好 là lời chào cơ bản."
  },
  {
    "type": "fill-in-the-blank",
    "question": "我____学生。",
    "options": [],
    "answer": "是",
    "explanation": "是 dùng để nói là/thì/là."
  },
  {
    "type": "multiple-choice",
    "question": "Câu nào nghĩa là \"Thank you\"?",
    "options": ["谢谢", "你好", "再见", "请问"],
    "answer": "谢谢",
    "explanation": "谢谢 nghĩa là cảm ơn."
  },
  {
    "type": "fill-in-the-blank",
    "question": "你____吗？",
    "options": [],
    "answer": "好吗",
    "explanation": "你好吗 là câu hỏi bạn có khỏe không."
  },
  {
    "type": "multiple-choice",
    "question": "Chọn câu đúng về tên.",
    "options": ["我叫安。", "我安叫。", "叫我安。", "安叫我。"],
    "answer": "我叫安。",
    "explanation": "我叫 + tên là câu giới thiệu tên."
  },
  {
    "type": "fill-in-the-blank",
    "question": "我____越南人。",
    "options": [],
    "answer": "是",
    "explanation": "Là người Việt Nam dùng 是."
  },
  {
    "type": "multiple-choice",
    "question": "Chọn câu tạm biệt.",
    "options": ["再见！", "谢谢！", "你好！", "请问！"],
    "answer": "再见！",
    "explanation": "再见 nghĩa là tạm biệt."
  },
  {
    "type": "fill-in-the-blank",
    "question": "这____我的朋友。",
    "options": [],
    "answer": "是",
    "explanation": "这/这是 dùng để giới thiệu đây là."
  },
  {
    "type": "multiple-choice",
    "question": "Chọn câu hỏi lịch sự.",
    "options": ["请问，洗手间在哪儿？", "洗手间你。", "在哪儿请问洗手间。", "我洗手间。"],
    "answer": "请问，洗手间在哪儿？",
    "explanation": "请问 làm câu hỏi lịch sự hơn."
  },
  {
    "type": "fill-in-the-blank",
    "question": "我____中文。",
    "options": [],
    "answer": "学",
    "explanation": "学中文 là học tiếng Trung."
  }
]
$json$::jsonb
WHERE "title" = 'ZH A1 - Daily Essentials';

UPDATE "Lesson"
SET "content" = $json$
[
  {
    "type": "multiple-choice",
    "question": "Chọn câu hỏi đường đúng.",
    "options": ["地铁站在哪儿？", "在哪儿地铁站吗。", "地铁站我。", "哪儿地铁站在。"],
    "answer": "地铁站在哪儿？",
    "explanation": "在哪儿 đặt cuối để hỏi ở đâu."
  },
  {
    "type": "fill-in-the-blank",
    "question": "请____我，这里怎么走？",
    "options": [],
    "answer": "问",
    "explanation": "请问 là cách hỏi lịch sự."
  },
  {
    "type": "multiple-choice",
    "question": "Câu nào nghĩa là \"turn left\"?",
    "options": ["向左拐", "向右拐", "一直走", "坐车"],
    "answer": "向左拐",
    "explanation": "向左拐 nghĩa là rẽ trái."
  },
  {
    "type": "fill-in-the-blank",
    "question": "一直____。",
    "options": [],
    "answer": "走",
    "explanation": "一直走 nghĩa là đi thẳng."
  },
  {
    "type": "multiple-choice",
    "question": "Chọn câu mua vé.",
    "options": ["我要买一张票。", "我票一张买要。", "买我要票一张。", "票买我一张。"],
    "answer": "我要买一张票。",
    "explanation": "我要买... là tôi muốn mua..."
  },
  {
    "type": "fill-in-the-blank",
    "question": "火车站离这里____吗？",
    "options": [],
    "answer": "远",
    "explanation": "远 nghĩa là xa."
  },
  {
    "type": "multiple-choice",
    "question": "Chọn câu đúng về taxi.",
    "options": ["我想打车。", "我打想车。", "车打我想。", "打车我了。"],
    "answer": "我想打车。",
    "explanation": "想 + động từ diễn tả muốn làm gì."
  },
  {
    "type": "fill-in-the-blank",
    "question": "到机场____多少钱？",
    "options": [],
    "answer": "要",
    "explanation": "要多少钱 hỏi cần bao nhiêu tiền."
  },
  {
    "type": "multiple-choice",
    "question": "Câu nào nghĩa là \"near here\"?",
    "options": ["在这儿附近", "很远", "一直走", "向右拐"],
    "answer": "在这儿附近",
    "explanation": "附近 nghĩa là gần."
  },
  {
    "type": "fill-in-the-blank",
    "question": "我____地图。",
    "options": [],
    "answer": "看",
    "explanation": "看地图 là xem bản đồ."
  }
]
$json$::jsonb
WHERE "title" = 'ZH A2 - Travel and Directions';
