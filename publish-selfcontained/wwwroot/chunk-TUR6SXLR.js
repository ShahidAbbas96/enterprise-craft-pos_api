import{Ga as w,Ka as f,Kb as $,Lb as E,Mb as T,Q as C,Rb as N,Ta as s,Tb as D,Va as I,W as S,Wa as _,Wb as L,Xa as b,Ya as a,Za as n,bb as m,ib as o,jb as c,kb as d,lb as P,ub as v,vb as g,ya as r}from"./chunk-WS3FZZMB.js";var A=(i,t)=>t.productId;function M(i,t){if(i&1&&(a(0,"div"),o(1),n()),i&2){let e=m();r(),c(e.store==null?null:e.store.address)}}function O(i,t){if(i&1&&o(0),i&2){let e=m(2);d(" Tel: ",e.store==null?null:e.store.phone," ")}}function Q(i,t){i&1&&o(0," \xB7 ")}function H(i,t){if(i&1&&o(0),i&2){let e=m(2);d(" ",e.store==null?null:e.store.email," ")}}function U(i,t){if(i&1&&(a(0,"div"),f(1,O,1,1)(2,Q,1,0)(3,H,1,1),n()),i&2){let e=m();r(),s(e.store!=null&&e.store.phone?1:-1),r(),s(e.store!=null&&e.store.phone&&(e.store!=null&&e.store.email)?2:-1),r(),s(e.store!=null&&e.store.email?3:-1)}}function G(i,t){if(i&1&&o(0),i&2){let e=m(2);d(" NTN # ",e.store==null?null:e.store.ntn," ")}}function W(i,t){if(i&1&&o(0),i&2){let e=m(2);d(" \xA0\xA0STRN # ",e.store==null?null:e.store.strn," ")}}function Y(i,t){if(i&1&&(a(0,"div"),f(1,G,1,1)(2,W,1,1),n()),i&2){let e=m();r(),s(e.store!=null&&e.store.ntn?1:-1),r(),s(e.store!=null&&e.store.strn?2:-1)}}function J(i,t){if(i&1&&(a(0,"div"),o(1),n()),i&2){let e=m();r(),d("Cashier: ",e.cashierName,"")}}function K(i,t){if(i&1&&(a(0,"div"),o(1),n()),i&2){let e=m();r(),d("Sales Person: ",e.salesPersonName,"")}}function V(i,t){if(i&1&&(a(0,"tr")(1,"td",17),o(2),n(),a(3,"td",18),o(4),n(),a(5,"td",19),o(6),v(7,"number"),n(),a(8,"td",17),o(9),n(),a(10,"td",19),o(11),v(12,"number"),n(),a(13,"td",19),o(14),v(15,"number"),n()()),i&2){let e=t.$implicit,p=t.$index,x=m(2);r(2),c(p+1),r(2),c(e.productName),r(2),c(g(7,6,e.unitPrice,"1.2-2")),r(3),c(e.quantity),r(2),c(g(12,9,x.lineDiscount(e),"1.2-2")),r(3),c(g(15,12,e.lineTotal,"1.2-2"))}}function X(i,t){if(i&1&&(a(0,"div",15),o(1),n()),i&2){let e=t.$implicit,p=t.$index;r(),P("",p+1,". ",e,"")}}function Z(i,t){if(i&1&&(a(0,"div",0)(1,"div",1)(2,"div",2),o(3,"Purchase Slip"),n(),a(4,"div",3),o(5),n(),f(6,M,2,1,"div")(7,U,4,3,"div")(8,Y,3,2,"div"),n(),a(9,"div",4)(10,"div"),o(11,"Invoice: "),a(12,"strong"),o(13),n()(),a(14,"div"),o(15),n(),a(16,"div"),o(17),v(18,"date"),n(),f(19,J,2,1,"div"),a(20,"div"),o(21),n(),f(22,K,2,1,"div"),n(),a(23,"table",5)(24,"thead")(25,"tr",6)(26,"th",7),o(27,"Sr"),n(),a(28,"th",8),o(29,"Product"),n(),a(30,"th",9),o(31,"Price"),n(),a(32,"th",7),o(33,"Qty"),n(),a(34,"th",9),o(35,"Disc"),n(),a(36,"th",9),o(37,"Total"),n()()(),a(38,"tbody"),_(39,V,16,15,"tr",null,A),n()(),a(41,"div",10)(42,"span"),o(43),n(),a(44,"span"),o(45),v(46,"number"),n()(),a(47,"div",11)(48,"div",12)(49,"span"),o(50),n(),a(51,"span"),o(52),v(53,"number"),n()(),a(54,"div",13)(55,"span"),o(56,"Net Total"),n(),a(57,"span"),o(58),v(59,"number"),n()()(),a(60,"div",14)(61,"div",3),o(62,"Refund & Exchange Policy:"),n(),_(63,X,2,2,"div",15,I),n(),a(65,"div",16),o(66," Thanks for shopping with us! "),n()()),i&2){let e=t,p=m();r(5),c(e.warehouseName),r(),s(e.store!=null&&e.store.address?6:-1),r(),s(e.store!=null&&e.store.phone||e.store!=null&&e.store.email?7:-1),r(),s(e.store!=null&&e.store.ntn||e.store!=null&&e.store.strn?8:-1),r(5),c(e.orderNumber),r(2),d("MOP: ",e.paymentMethod,""),r(2),d("Date: ",g(18,15,e.createdAtUtc,"medium"),""),r(2),s(e.cashierName?19:-1),r(2),d("Customer: ",e.customerName||"Walk-in",""),r(),s(e.salesPersonName?22:-1),r(17),b(e.lines),r(4),d("Gross Total (",p.totalQuantity,")"),r(2),c(g(46,18,e.subtotal,"1.2-2")),r(5),d("Discount",e.discountLabel?" ("+e.discountLabel+")":"",""),r(2),d("-",g(53,21,e.discountAmount,"1.2-2"),""),r(6),c(g(59,24,e.total,"1.2-2")),r(5),b(p.policyLines)}}var k=["Goods once sold may be exchanged within 15 days of purchase.","Items must be unused, unwashed, and in their original packing.","This receipt is required for any exchange or claim.","Sale/clearance-priced items are not eligible for exchange."],j=class i{sale=null;defaultPolicyLines=k;lineDiscount(t){return F(t.unitPrice*t.quantity*t.discountPercent/100)}get totalQuantity(){return this.sale?.lines.reduce((t,e)=>t+e.quantity,0)??0}get policyLines(){return B(this.sale?.store?.footerText)}toPrintableHtml(){if(!this.sale)return"";let t=this.sale,e=t.store,p=t.lines.map((u,h)=>`
          <tr>
            <td style="padding:3px 0;text-align:center">${h+1}</td>
            <td style="padding:3px 0">${l(u.productName)}</td>
            <td style="padding:3px 0;text-align:right">${u.unitPrice.toFixed(2)}</td>
            <td style="padding:3px 0;text-align:center">${u.quantity}</td>
            <td style="padding:3px 0;text-align:right">${F(u.unitPrice*u.quantity*u.discountPercent/100).toFixed(2)}</td>
            <td style="padding:3px 0;text-align:right">${u.lineTotal.toFixed(2)}</td>
          </tr>`).join(""),x=t.lines.reduce((u,h)=>u+h.quantity,0),R=B(e?.footerText).map((u,h)=>`<div style="margin-top:2px">${h+1}. ${l(u)}</div>`).join("");return`
      <div style="font-family:'Courier New',monospace;width:300px;margin:0 auto;font-size:11px;color:#111">
        <div style="text-align:center;margin-bottom:8px">
          <div style="font-size:15px;font-weight:700">Purchase Slip</div>
          <div style="font-weight:700">${l(t.warehouseName)}</div>
          ${e?.address?`<div>${l(e.address)}</div>`:""}
          ${e?.phone||e?.email?`<div>${e.phone?`Tel: ${l(e.phone)}`:""}${e.phone&&e.email?" &middot; ":""}${e.email?l(e.email):""}</div>`:""}
          ${e?.ntn||e?.strn?`<div>${e.ntn?`NTN # ${l(e.ntn)}`:""}${e.ntn&&e.strn?"  ":""}${e.strn?`STRN # ${l(e.strn)}`:""}</div>`:""}
        </div>
        <div style="border-top:1px dashed #000;border-bottom:1px dashed #000;padding:6px 0;margin-bottom:6px;display:grid;grid-template-columns:1fr 1fr;gap:2px 8px">
          <div>Invoice: <strong>${l(t.orderNumber)}</strong></div>
          <div>MOP: ${l(t.paymentMethod)}</div>
          <div>Date: ${new Date(t.createdAtUtc).toLocaleString()}</div>
          ${t.cashierName?`<div>Cashier: ${l(t.cashierName)}</div>`:"<div></div>"}
          <div>Customer: ${l(t.customerName??"Walk-in")}</div>
          ${t.salesPersonName?`<div>Sales Person: ${l(t.salesPersonName)}</div>`:""}
        </div>
        <table style="width:100%;border-collapse:collapse">
          <thead>
            <tr style="border-bottom:1px solid #000">
              <th style="text-align:center;padding-bottom:4px">Sr</th>
              <th style="text-align:left;padding-bottom:4px">Product</th>
              <th style="text-align:right;padding-bottom:4px">Price</th>
              <th style="text-align:center;padding-bottom:4px">Qty</th>
              <th style="text-align:right;padding-bottom:4px">Disc</th>
              <th style="text-align:right;padding-bottom:4px">Total</th>
            </tr>
          </thead>
          <tbody>${p}</tbody>
        </table>
        <div style="border-top:1px solid #000;margin-top:4px;padding-top:4px;display:flex;justify-content:space-between;font-weight:700">
          <span>Gross Total (${x})</span><span>${t.subtotal.toFixed(2)}</span>
        </div>
        <div style="border-top:1px dashed #000;margin-top:6px;padding-top:6px">
          <div style="display:flex;justify-content:space-between"><span>Discount${t.discountLabel?` (${l(t.discountLabel)})`:""}</span><span>-${t.discountAmount.toFixed(2)}</span></div>
          <div style="display:flex;justify-content:space-between;font-weight:700;font-size:13px;border-top:1px solid #000;margin-top:4px;padding-top:4px">
            <span>Net Total</span><span>${t.total.toFixed(2)}</span>
          </div>
        </div>
        <div style="border-top:1px dashed #000;margin-top:8px;padding-top:6px;font-size:10px">
          <div style="font-weight:700">Refund &amp; Exchange Policy:</div>
          ${R}
        </div>
        <div style="text-align:center;margin-top:10px;border-top:1px dashed #000;padding-top:6px">
          Thanks for shopping with us!
        </div>
      </div>
    `}static \u0275fac=function(e){return new(e||i)};static \u0275cmp=w({type:i,selectors:[["app-invoice-preview"]],inputs:{sale:"sale"},decls:1,vars:1,consts:[[2,"font-family","'Courier New', monospace","width","300px","margin","0 auto","font-size","11px","color","#111","background","white","padding","1rem","border-radius","var(--radius-md)","border","1px solid var(--border)"],[2,"text-align","center","margin-bottom","0.5rem"],[2,"font-size","1.05rem","font-weight","700"],[2,"font-weight","700"],[2,"border-top","1px dashed #000","border-bottom","1px dashed #000","padding","0.4rem 0","margin-bottom","0.4rem","display","grid","grid-template-columns","1fr 1fr","gap","0.1rem 0.5rem"],[2,"width","100%","border-collapse","collapse"],[2,"border-bottom","1px solid #000"],[2,"text-align","center","padding-bottom","0.25rem"],[2,"text-align","left","padding-bottom","0.25rem"],[2,"text-align","right","padding-bottom","0.25rem"],[2,"border-top","1px solid #000","margin-top","0.25rem","padding-top","0.25rem","display","flex","justify-content","space-between","font-weight","700"],[2,"border-top","1px dashed #000","margin-top","0.4rem","padding-top","0.4rem"],[2,"display","flex","justify-content","space-between"],[2,"display","flex","justify-content","space-between","font-weight","700","font-size","0.95rem","border-top","1px solid #000","margin-top","0.25rem","padding-top","0.25rem"],[2,"border-top","1px dashed #000","margin-top","0.6rem","padding-top","0.4rem","font-size","0.68rem"],[2,"margin-top","0.1rem"],[2,"text-align","center","margin-top","0.6rem","border-top","1px dashed #000","padding-top","0.4rem"],[2,"padding","0.2rem 0","text-align","center"],[2,"padding","0.2rem 0"],[2,"padding","0.2rem 0","text-align","right"]],template:function(e,p){if(e&1&&f(0,Z,67,27,"div",0),e&2){let x;s((x=p.sale)?0:-1,x)}},dependencies:[T,E,$],encapsulation:2})};function B(i){return!i||i.trim().length===0?k:i.split(`
`).map(t=>t.trim().replace(/^\d+[.)]\s*/,"")).filter(t=>t.length>0)}function F(i){return Math.round(i*100)/100}function l(i){return i.replace(/[&<>"']/g,t=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"})[t])}var y=`${L.apiBaseUrl}/sales`,q=class i{http=S(D);list(t){let e=new N;for(let[p,x]of Object.entries(t))x!=null&&x!==""&&(e=e.set(p,String(x)));return this.http.get(y,{params:e})}create(t){return this.http.post(y,t)}get(t){return this.http.get(`${y}/${t}`)}static \u0275fac=function(e){return new(e||i)};static \u0275prov=C({token:i,factory:i.\u0275fac,providedIn:"root"})};export{j as a,q as b};
