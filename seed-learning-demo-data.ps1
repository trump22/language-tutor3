param(
    [string]$BaseUrl = 'http://localhost:63592/api',
    [string]$AdminEmail = 'admin@languagetutor.local',
    [string]$AdminPassword = 'Admin@123456'
)

$ErrorActionPreference = 'Stop'

function Invoke-ApiJson {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Url,
        [string]$Token,
        $Body
    )

    $headers = @{}
    if ($Token) {
        $headers.Authorization = "Bearer $Token"
    }

    $params = @{
        Uri = $Url
        Method = $Method
        Headers = $headers
    }

    if ($null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = ($Body | ConvertTo-Json -Depth 30)
    }

    Invoke-RestMethod @params
}

function New-Mcq {
    param([string]$Question, [string[]]$Options, [string]$Answer, [string]$Explanation)
    @{
        type = 'multiple-choice'
        question = $Question
        options = $Options
        answer = $Answer
        explanation = $Explanation
    }
}

function New-Blank {
    param([string]$Question, [string]$Answer, [string]$Explanation)
    @{
        type = 'fill-in-the-blank'
        question = $Question
        options = @()
        answer = $Answer
        explanation = $Explanation
    }
}

function Get-OrCreateCourse {
    param([string]$Title, [string]$Description, [string]$Language)

    $coursesResponse = Invoke-ApiJson -Method Get -Url "$BaseUrl/courses"
    $courses = @($coursesResponse.data)
    $existing = $courses | Where-Object { $_.title -eq $Title } | Select-Object -First 1
    if ($existing) {
        return $existing
    }

    $created = Invoke-ApiJson -Method Post -Url "$BaseUrl/courses" -Token $adminToken -Body @{
        title = $Title
        description = $Description
        language = $Language
    }
    return $created.data
}

function Get-OrCreateLesson {
    param([string]$CourseId, [string]$Title, [object[]]$Content)

    $courseDetail = Invoke-ApiJson -Method Get -Url "$BaseUrl/courses/$CourseId" -Token $adminToken
    $existing = @($courseDetail.data.lessons) | Where-Object { $_.title -eq $Title } | Select-Object -First 1
    if ($existing) {
        return $existing
    }

    $created = Invoke-ApiJson -Method Post -Url "$BaseUrl/courses/lessons/ai-generate" -Token $adminToken -Body @{
        courseId = $CourseId
        title = $Title
        content = $Content
    }
    return $created.data
}

$adminLogin = Invoke-ApiJson -Method Post -Url "$BaseUrl/auth/login" -Body @{
    email = $AdminEmail
    password = $AdminPassword
}
$adminToken = $adminLogin.token

$englishCourse = Get-OrCreateCourse `
    -Title 'Adaptive English Path' `
    -Description 'Seeded English learning path with beginner, intermediate and advanced grammar/vocabulary lessons.' `
    -Language 'EN'

$chineseCourse = Get-OrCreateCourse `
    -Title 'Adaptive Chinese Path' `
    -Description 'Seeded Chinese learning path with daily communication, travel and workplace topics.' `
    -Language 'ZH'

$lessonDefinitions = @(
    @{
        courseId = $englishCourse.id
        title = 'EN A1 - Daily Introductions'
        content = @(
            (New-Mcq 'Choose the best greeting for the morning.' @('Good morning!', 'Good night!', 'See you later.', 'I am fine.') 'Good morning!' 'Good morning dung cho buoi sang.'),
            (New-Blank 'My name ____ Anna.' 'is' 'Sau My name dung dong tu be la is.'),
            (New-Mcq 'Choose the correct question.' @('What is your name?', 'What your name is?', 'Name what you?', 'Your is name?') 'What is your name?' 'Cau hoi dung la What is your name?'),
            (New-Blank 'I ____ from Vietnam.' 'am' 'Chu ngu I dung voi am.'),
            (New-Mcq 'Choose the polite answer.' @('Nice to meet you.', 'No meet.', 'I name.', 'Goodbye yesterday.') 'Nice to meet you.' 'Nice to meet you la cau dap lich su khi gap lan dau.'),
            (New-Blank 'She ____ a student.' 'is' 'She dung voi is.'),
            (New-Mcq 'Choose the correct sentence.' @('I live in Hanoi.', 'I lives in Hanoi.', 'I living Hanoi.', 'I am live Hanoi.') 'I live in Hanoi.' 'Voi I o hien tai don, dong tu giu nguyen.'),
            (New-Blank 'How ____ you?' 'are' 'Cau hoi suc khoe dung How are you?'),
            (New-Mcq 'Choose a self-introduction.' @('I am Minh.', 'Are Minh I.', 'Minh I am?', 'I Minh be.') 'I am Minh.' 'I am + ten la cau gioi thieu co ban.'),
            (New-Blank 'This ____ my friend.' 'is' 'This dung voi is.')
        )
    },
    @{
        courseId = $englishCourse.id
        title = 'EN A2 - Food and Ordering'
        content = @(
            (New-Mcq 'Choose the polite order.' @('Can I have a coffee, please?', 'Give coffee.', 'Coffee now.', 'I coffee need.') 'Can I have a coffee, please?' 'Can I have...please la cach goi mon lich su.'),
            (New-Blank 'I would ____ a sandwich.' 'like' 'Would like dung de noi mong muon lich su.'),
            (New-Mcq 'What does "bill" mean in a restaurant?' @('The amount to pay', 'A table', 'A spoon', 'A menu picture') 'The amount to pay' 'Bill la hoa don.'),
            (New-Blank 'Could I see the ____, please?' 'menu' 'Menu la thuc don.'),
            (New-Mcq 'Choose the correct sentence.' @('I am allergic to peanuts.', 'I allergy peanuts.', 'Peanuts allergic I.', 'I am peanut allergy.') 'I am allergic to peanuts.' 'Cau truc dung: be allergic to.'),
            (New-Blank 'This soup is too ____.' 'salty' 'Salty nghia la man.'),
            (New-Mcq 'Choose the waiter question.' @('Are you ready to order?', 'Ready order you?', 'Do ready order?', 'You order ready are?') 'Are you ready to order?' 'Cau hoi dung voi be: Are you ready...?'),
            (New-Blank 'I will pay ____ card.' 'by' 'Pay by card la thanh toan bang the.'),
            (New-Mcq 'Choose the best reply to "Anything else?"' @('No, thank you.', 'Yes yesterday.', 'I table.', 'Else no food?') 'No, thank you.' 'Cau dap ngan gon va lich su.'),
            (New-Blank 'The food ____ delicious.' 'is' 'Food la danh tu khong dem duoc trong cau nay, dung is.')
        )
    },
    @{
        courseId = $englishCourse.id
        title = 'EN B1 - Workplace Communication'
        content = @(
            (New-Mcq 'Choose the best meeting opener.' @('Thanks for joining the meeting.', 'Join meeting thanks you.', 'Meeting is joined.', 'Thanks meeting for join.') 'Thanks for joining the meeting.' 'Cau mo dau hop tu nhien.'),
            (New-Blank 'Could you ____ the report by Friday?' 'send' 'Could you + V nguyen mau.'),
            (New-Mcq 'Choose a professional disagreement.' @('I see your point, but I have a different view.', 'No, you are wrong.', 'Bad idea.', 'I disagree because no.') 'I see your point, but I have a different view.' 'Cach bat dong lich su va chuyen nghiep.'),
            (New-Blank 'Let us ____ the next steps.' 'discuss' 'Let us + V nguyen mau.'),
            (New-Mcq 'What does "deadline" mean?' @('The final time to finish work', 'A phone call', 'A salary', 'A meeting room') 'The final time to finish work' 'Deadline la han chot.'),
            (New-Blank 'I am following ____ on your email.' 'up' 'Follow up la tiep tuc theo doi/hoi lai.'),
            (New-Mcq 'Choose the correct sentence.' @('We need to reschedule the meeting.', 'We need reschedule meeting.', 'We need to rescheduling.', 'We are need reschedule.') 'We need to reschedule the meeting.' 'Need to + V nguyen mau.'),
            (New-Blank 'Please keep me ____.' 'updated' 'Keep someone updated la cap nhat thong tin cho ai.'),
            (New-Mcq 'Choose the best closing.' @('Please let me know if you have any questions.', 'Question me if have.', 'Know me questions.', 'Any questions let please.') 'Please let me know if you have any questions.' 'Cau ket email chuyen nghiep.'),
            (New-Blank 'The project is ____ schedule.' 'on' 'On schedule nghia la dung tien do.')
        )
    },
    @{
        courseId = $englishCourse.id
        title = 'EN C1 - Academic Discussion'
        content = @(
            (New-Mcq 'Choose the strongest thesis phrase.' @('This essay argues that...', 'I maybe think...', 'This is about stuff.', 'The thing says...') 'This essay argues that...' 'This phrase phu hop van phong hoc thuat.'),
            (New-Blank 'The evidence ____ the main argument.' 'supports' 'Evidence so it dung supports.'),
            (New-Mcq 'Choose a contrast connector.' @('However', 'Also', 'Because', 'For example') 'However' 'However dung de chuyen y tuong doi lap.'),
            (New-Blank 'The results are consistent ____ previous studies.' 'with' 'Consistent with la cum dung.'),
            (New-Mcq 'Choose the precise word.' @('significant', 'big-big', 'nice', 'kind of good') 'significant' 'Significant trang trong va chinh xac hon.'),
            (New-Blank 'This assumption should be ____ carefully.' 'examined' 'Should be + V3/passive.'),
            (New-Mcq 'Choose the cautious claim.' @('The findings suggest a possible link.', 'This proves everything.', 'It is always true.', 'No other reason exists.') 'The findings suggest a possible link.' 'Hoc thuat can than trong khi dua ket luan.'),
            (New-Blank 'The author fails to ____ alternative explanations.' 'consider' 'Fail to + V nguyen mau.'),
            (New-Mcq 'Choose the best paraphrase of "increase".' @('rise', 'make', 'go thing', 'put') 'rise' 'Rise co nghia tang len.'),
            (New-Blank 'In conclusion, the study ____ further research.' 'requires' 'Study so it dung requires.')
        )
    },
    @{
        courseId = $chineseCourse.id
        title = 'ZH A1 - Daily Essentials'
        content = @(
            (New-Mcq 'Chon cau chao dung.' @('你好！', '我忙饭。', '中文我。', '谢谢吗。') '你好！' '你好 la loi chao co ban.'),
            (New-Blank '我____学生。' '是' '是 dung de noi la/thi/la.'),
            (New-Mcq 'Cau nao nghia la "Thank you"?' @('谢谢', '你好', '再见', '请问') '谢谢' '谢谢 nghia la cam on.'),
            (New-Blank '你____吗？' '好吗' '你好吗 la cau hoi ban co khoe khong.'),
            (New-Mcq 'Chon cau dung ve ten.' @('我叫安。', '我安叫。', '叫我安。', '安叫我。') '我叫安。' '我叫 + ten la cau gioi thieu ten.'),
            (New-Blank '我____越南人。' '是' 'La nguoi Viet Nam dung 是.'),
            (New-Mcq 'Chon cau tam biet.' @('再见！', '谢谢！', '你好！', '请问！') '再见！' '再见 nghia la tam biet.'),
            (New-Blank '这____我的朋友。' '是' '这/这是 dung de gioi thieu day la.'),
            (New-Mcq 'Chon cau hoi lich su.' @('请问，洗手间在哪儿？', '洗手间你。', '在哪儿请问洗手间。', '我洗手间。') '请问，洗手间在哪儿？' '请问 lam cau hoi lich su hon.'),
            (New-Blank '我____中文。' '学' '学中文 la hoc tieng Trung.')
        )
    },
    @{
        courseId = $chineseCourse.id
        title = 'ZH A2 - Travel and Directions'
        content = @(
            (New-Mcq 'Chon cau hoi duong dung.' @('地铁站在哪儿？', '在哪儿地铁站吗。', '地铁站我。', '哪儿地铁站在。') '地铁站在哪儿？' '在哪儿 dat cuoi de hoi o dau.'),
            (New-Blank '请____我，这里怎么走？' '问' '请问 la cach hoi lich su.'),
            (New-Mcq 'Cau nao nghia la "turn left"?' @('向左拐', '向右拐', '一直走', '坐车') '向左拐' '向左拐 nghia la re trai.'),
            (New-Blank '一直____。' '走' '一直走 nghia la di thang.'),
            (New-Mcq 'Chon cau mua ve.' @('我要买一张票。', '我票一张买要。', '买我要票一张。', '票买我一张。') '我要买一张票。' '我要买... la toi muon mua...'),
            (New-Blank '火车站离这里____吗？' '远' '远 nghia la xa.'),
            (New-Mcq 'Chon cau dung ve taxi.' @('我想打车。', '我打想车。', '车打我想。', '打车我了。') '我想打车。' '想 + dong tu dien ta muon lam gi.'),
            (New-Blank '到机场____多少钱？' '要' '要多少钱 hoi can bao nhieu tien.'),
            (New-Mcq 'Cau nao nghia la "near here"?' @('在这儿附近', '很远', '一直走', '向右拐') '在这儿附近' '附近 nghia la gan.'),
            (New-Blank '我____地图。' '看' '看地图 la xem ban do.')
        )
    }
)

$lessons = @()
foreach ($definition in $lessonDefinitions) {
    $lesson = Get-OrCreateLesson -CourseId $definition.courseId -Title $definition.title -Content $definition.content
    $lessons += $lesson
}

$students = @(
    @{ n='Nguyen An'; lang='EN'; level='Beginner'; goal='Build daily conversation confidence' },
    @{ n='Tran Binh'; lang='EN'; level='Beginner'; goal='Improve grammar basics' },
    @{ n='Le Chi'; lang='ZH'; level='Beginner'; goal='Start Chinese daily greetings' },
    @{ n='Pham Dung'; lang='EN'; level='Beginner'; goal='Speak at school more naturally' },
    @{ n='Hoang Giang'; lang='ZH'; level='Beginner'; goal='Learn survival Chinese for travel' },
    @{ n='Do Hanh'; lang='EN'; level='Intermediate'; goal='Order food and travel in English' },
    @{ n='Vu Khoa'; lang='EN'; level='Intermediate'; goal='Workplace English communication' },
    @{ n='Dang Linh'; lang='ZH'; level='Intermediate'; goal='Ask directions and travel independently' },
    @{ n='Bui Minh'; lang='EN'; level='Intermediate'; goal='Write professional emails' },
    @{ n='Phan Nhi'; lang='ZH'; level='Intermediate'; goal='Prepare for HSK speaking topics' },
    @{ n='Nguyen Quang'; lang='EN'; level='Advanced'; goal='Join academic discussions' },
    @{ n='Tran Son'; lang='EN'; level='Advanced'; goal='Improve presentation language' },
    @{ n='Le Trang'; lang='ZH'; level='Intermediate'; goal='Improve Chinese grammar accuracy' },
    @{ n='Pham Uyen'; lang='EN'; level='Advanced'; goal='Use nuanced vocabulary' },
    @{ n='Hoang Van'; lang='EN'; level='Intermediate'; goal='Speak fluently at work' },
    @{ n='Do Xuan'; lang='ZH'; level='Beginner'; goal='Strengthen tones and daily phrases' },
    @{ n='Vu Yen'; lang='EN'; level='Beginner'; goal='Recover grammar foundation' },
    @{ n='Dang Bao'; lang='EN'; level='Intermediate'; goal='Improve listening and meetings' },
    @{ n='Bui Cam'; lang='ZH'; level='Intermediate'; goal='Travel conversations in Chinese' },
    @{ n='Phan Dat'; lang='EN'; level='Advanced'; goal='Academic writing and debate' }
)

$password = 'Student@123456'
$createdStudents = @()

for ($i = 0; $i -lt $students.Count; $i++) {
    $index = $i + 1
    $email = ('seed.student{0:D2}@languagetutor.local' -f $index)
    $student = $students[$i]

    try {
        $register = Invoke-ApiJson -Method Post -Url "$BaseUrl/auth/register" -Body @{
            email = $email
            password = $password
            name = $student.n
            phoneNumber = ('090100{0:D4}' -f $index)
            address = "Seed District $index"
            dateOfBirth = ('200{0}-0{1}-15' -f ($index % 5), (($index % 9) + 1))
            languagePreference = $student.lang
            skillLevel = $student.level
            learningGoal = $student.goal
        }
        $user = $register.user
    }
    catch {
        $found = Invoke-ApiJson -Method Get -Url "$BaseUrl/admin/users/search?q=$email" -Token $adminToken
        $user = @($found) | Select-Object -First 1
    }

    $login = Invoke-ApiJson -Method Post -Url "$BaseUrl/auth/login" -Body @{
        email = $email
        password = $password
    }

    $createdStudents += @{
        email = $email
        token = $login.token
        user = $login.user
        level = $student.level
        lang = $student.lang
    }
}

for ($i = 0; $i -lt $createdStudents.Count; $i++) {
    $student = $createdStudents[$i]
    $completedCount = switch ($student.level) {
        'Beginner' { 1 + ($i % 3) }
        'Intermediate' { 3 + ($i % 3) }
        'Advanced' { 4 + ($i % 2) }
        default { 2 }
    }

    $preferredLessons = if ($student.lang -eq 'ZH') {
        @($lessons | Where-Object { $_.title -like 'ZH *' })
    } else {
        @($lessons | Where-Object { $_.title -like 'EN *' })
    }

    if ($student.level -eq 'Advanced' -and $student.lang -eq 'EN') {
        $preferredLessons = @($lessons | Where-Object { $_.title -like 'EN *' })
    }

    $targetLessons = @($preferredLessons | Select-Object -First ([Math]::Min($completedCount, $preferredLessons.Count)))

    for ($j = 0; $j -lt $targetLessons.Count; $j++) {
        $baseScore = switch ($student.level) {
            'Beginner' { 4 + (($i + $j) % 4) }
            'Intermediate' { 5 + (($i + $j) % 4) }
            'Advanced' { 7 + (($i + $j) % 4) }
            default { 6 }
        }

        if (($i % 5) -eq 0 -and $j -eq 0) {
            $baseScore = [Math]::Max(3, $baseScore - 2)
        }
        if (($i % 4) -eq 0 -and $j -eq ($targetLessons.Count - 1)) {
            $baseScore = [Math]::Min(10, $baseScore + 2)
        }

        $score = [Math]::Max(2, [Math]::Min(10, $baseScore))
        $completionTime = 300 + (($i + 1) * 23) + ($j * 45)

        Invoke-ApiJson -Method Post -Url "$BaseUrl/courses/lessons/$($targetLessons[$j].id)/score" -Token $student.token -Body @{
            score = $score
            totalQuestions = 10
            completionTime = $completionTime
        } | Out-Null
    }
}

$summary = [ordered]@{
    coursesCreatedOrReused = 2
    lessonsCreatedOrReused = $lessons.Count
    exercisesPerLesson = 10
    studentsCreatedOrReused = $createdStudents.Count
    studentPassword = $password
    firstStudentEmail = 'seed.student01@languagetutor.local'
    lastStudentEmail = 'seed.student20@languagetutor.local'
}

$summary | ConvertTo-Json -Depth 5
